using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using AutoDocOps.ILScanner.Logging;
using System.Text.RegularExpressions;

namespace AutoDocOps.ILScanner.Services;

public class RoslynAnalyzer
{
    private readonly ILogger<RoslynAnalyzer> _logger;

    public RoslynAnalyzer(ILogger<RoslynAnalyzer> logger)
    {
        _logger = logger;
    }

    public async Task<ProjectMetadata> AnalyzeProjectAsync(
        string projectPath,
        string projectName,
        List<string> sourceFiles,
        string targetFramework,
        CancellationToken cancellationToken = default)
    {
    ArgumentNullException.ThrowIfNull(projectPath);
    ArgumentNullException.ThrowIfNull(projectName);
    ArgumentNullException.ThrowIfNull(sourceFiles);
    ArgumentNullException.ThrowIfNull(targetFramework);
        var metadata = new ProjectMetadata
        {
            ProjectName = projectName,
            TargetFramework = targetFramework,
            AnalysisTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        var totalLines = 0;

        foreach (var sourceFile in sourceFiles)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                var content = await File.ReadAllTextAsync(sourceFile, cancellationToken).ConfigureAwait(false);
                var lines = content.Split('\n').Length;
                totalLines += lines;

                var syntaxTree = CSharpSyntaxTree.ParseText(content, path: sourceFile, cancellationToken: cancellationToken);
                var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

                // Analyze classes
                var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
                foreach (var classDecl in classDeclarations)
                {
                    var classMetadata = AnalyzeClass(classDecl, sourceFile);
                    metadata.Classes.Add(classMetadata);
                }

                // Analyze interfaces
                var interfaceDeclarations = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>();
                foreach (var interfaceDecl in interfaceDeclarations)
                {
                    var interfaceMetadata = AnalyzeInterface(interfaceDecl, sourceFile);
                    metadata.Interfaces.Add(interfaceMetadata);
                }

                // Analyze standalone methods (in static classes or top-level programs)
                var methodDeclarations = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                    .Where(m => m.Parent is not ClassDeclarationSyntax && m.Parent is not InterfaceDeclarationSyntax);
                foreach (var methodDecl in methodDeclarations)
                {
                    var methodMetadata = AnalyzeMethod(methodDecl, sourceFile);
                    metadata.Methods.Add(methodMetadata);
                }

                // Analyze standalone properties
                var propertyDeclarations = root.DescendantNodes().OfType<PropertyDeclarationSyntax>()
                    .Where(p => p.Parent is not ClassDeclarationSyntax && p.Parent is not InterfaceDeclarationSyntax);
                foreach (var propertyDecl in propertyDeclarations)
                {
                    var propertyMetadata = AnalyzeProperty(propertyDecl, sourceFile);
                    metadata.Properties.Add(propertyMetadata);
                }
            }
            catch (Exception ex)
            {
                _logger.FileAnalysisWarning(ex, sourceFile);
            }
        }

        metadata.TotalLines = totalLines;
        return metadata;
    }

    private ClassMetadata AnalyzeClass(ClassDeclarationSyntax classDecl, string filePath)
    {
        var classMetadata = new ClassMetadata
        {
            Name = classDecl.Identifier.ValueText,
            Namespace = GetNamespace(classDecl),
            AccessModifier = GetAccessModifier(classDecl.Modifiers),
            IsAbstract = classDecl.Modifiers.Any(SyntaxKind.AbstractKeyword),
            IsSealed = classDecl.Modifiers.Any(SyntaxKind.SealedKeyword),
            Documentation = GetDocumentationComment(classDecl),
            FilePath = filePath,
            LineNumber = classDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1
        };

        // Base class
        if (classDecl.BaseList?.Types.FirstOrDefault()?.Type is IdentifierNameSyntax baseType)
        {
            classMetadata.BaseClass = baseType.Identifier.ValueText;
        }

        // Interfaces
        var interfaces = classDecl.BaseList?.Types
            .Skip(classMetadata.BaseClass != null ? 1 : 0)
            .Select(t => t.Type.ToString()) ?? Enumerable.Empty<string>();
        classMetadata.Interfaces.AddRange(interfaces);

        // Methods
        var methods = classDecl.Members.OfType<MethodDeclarationSyntax>();
        foreach (var method in methods)
        {
            classMetadata.Methods.Add(AnalyzeMethod(method, filePath));
        }

        // Properties
        var properties = classDecl.Members.OfType<PropertyDeclarationSyntax>();
        foreach (var property in properties)
        {
            classMetadata.Properties.Add(AnalyzeProperty(property, filePath));
        }

        return classMetadata;
    }

    private InterfaceMetadata AnalyzeInterface(InterfaceDeclarationSyntax interfaceDecl, string filePath)
    {
        var interfaceMetadata = new InterfaceMetadata
        {
            Name = interfaceDecl.Identifier.ValueText,
            Namespace = GetNamespace(interfaceDecl),
            AccessModifier = GetAccessModifier(interfaceDecl.Modifiers),
            Documentation = GetDocumentationComment(interfaceDecl),
            FilePath = filePath,
            LineNumber = interfaceDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1
        };

        // Base interfaces
        var baseInterfaces = interfaceDecl.BaseList?.Types
            .Select(t => t.Type.ToString()) ?? Enumerable.Empty<string>();
        interfaceMetadata.BaseInterfaces.AddRange(baseInterfaces);

        // Methods
        var methods = interfaceDecl.Members.OfType<MethodDeclarationSyntax>();
        foreach (var method in methods)
        {
            interfaceMetadata.Methods.Add(AnalyzeMethod(method, filePath));
        }

        // Properties
        var properties = interfaceDecl.Members.OfType<PropertyDeclarationSyntax>();
        foreach (var property in properties)
        {
            interfaceMetadata.Properties.Add(AnalyzeProperty(property, filePath));
        }

        return interfaceMetadata;
    }

    private MethodMetadata AnalyzeMethod(MethodDeclarationSyntax methodDecl, string filePath)
    {
        var methodMetadata = new MethodMetadata
        {
            Name = methodDecl.Identifier.ValueText,
            ReturnType = methodDecl.ReturnType.ToString(),
            AccessModifier = GetAccessModifier(methodDecl.Modifiers),
            IsStatic = methodDecl.Modifiers.Any(SyntaxKind.StaticKeyword),
            IsVirtual = methodDecl.Modifiers.Any(SyntaxKind.VirtualKeyword),
            IsOverride = methodDecl.Modifiers.Any(SyntaxKind.OverrideKeyword),
            IsAsync = methodDecl.Modifiers.Any(SyntaxKind.AsyncKeyword),
            Documentation = GetDocumentationComment(methodDecl),
            FilePath = filePath,
            LineNumber = methodDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1
        };

        // Parameters
        foreach (var param in methodDecl.ParameterList.Parameters)
        {
            var paramMetadata = new ParameterMetadata
            {
                Name = param.Identifier.ValueText,
                Type = param.Type?.ToString() ?? "var",
                IsOptional = param.Default != null,
                DefaultValue = param.Default?.Value.ToString() ?? string.Empty
            };
            methodMetadata.Parameters.Add(paramMetadata);
        }

        return methodMetadata;
    }

    private PropertyMetadata AnalyzeProperty(PropertyDeclarationSyntax propertyDecl, string filePath)
    {
        var propertyMetadata = new PropertyMetadata
        {
            Name = propertyDecl.Identifier.ValueText,
            Type = propertyDecl.Type.ToString(),
            AccessModifier = GetAccessModifier(propertyDecl.Modifiers),
            IsStatic = propertyDecl.Modifiers.Any(SyntaxKind.StaticKeyword),
            Documentation = GetDocumentationComment(propertyDecl),
            FilePath = filePath,
            LineNumber = propertyDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1
        };

        // Check for getter and setter
        if (propertyDecl.AccessorList != null)
        {
            propertyMetadata.HasGetter = propertyDecl.AccessorList.Accessors
                .Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
            propertyMetadata.HasSetter = propertyDecl.AccessorList.Accessors
                .Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration) || a.IsKind(SyntaxKind.InitAccessorDeclaration));
        }
        else if (propertyDecl.ExpressionBody != null)
        {
            // Expression-bodied property (getter only)
            propertyMetadata.HasGetter = true;
            propertyMetadata.HasSetter = false;
        }

        return propertyMetadata;
    }

    private string GetNamespace(SyntaxNode node)
    {
        var namespaceDecl = node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        return namespaceDecl?.Name.ToString() ?? string.Empty;
    }

    private string GetAccessModifier(SyntaxTokenList modifiers)
    {
        if (modifiers.Any(SyntaxKind.PublicKeyword))
        {
            return "public";
        }
        if (modifiers.Any(SyntaxKind.PrivateKeyword))
        {
            return "private";
        }
        if (modifiers.Any(SyntaxKind.ProtectedKeyword))
        {
            return "protected";
        }
        if (modifiers.Any(SyntaxKind.InternalKeyword))
        {
            return "internal";
        }
        return "internal"; // Default in C#
    }

    private string GetDocumentationComment(SyntaxNode node)
    {
        var documentationComment = node.GetLeadingTrivia()
            .Where(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || 
                       t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            .FirstOrDefault();

        if (documentationComment.IsKind(SyntaxKind.None))
        {
            return string.Empty;
        }

        var text = documentationComment.ToString();
        
        // Clean up XML documentation
        text = Regex.Replace(text, @"^\s*///\s?", "", RegexOptions.Multiline);
        text = Regex.Replace(text, @"<[^>]+>", ""); // Remove XML tags
        text = text.Trim();

        return text;
    }
}

