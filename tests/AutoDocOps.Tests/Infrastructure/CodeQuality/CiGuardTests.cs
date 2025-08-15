using Xunit;
using System.IO;
using System.Linq;

namespace AutoDocOps.Tests.Infrastructure.CodeQuality;

public class CiGuardTests
{
    [Fact]
    public void CiGuard_Runs()
    {
        // Test de humo que siempre corre en CI
        // Verifica que al menos un proyecto de tests existe
        var currentDir = AppContext.BaseDirectory;
        var solutionRoot = FindSolutionRoot(currentDir);
        var testProjects = Directory.GetFiles(solutionRoot, "*.Tests.csproj", SearchOption.AllDirectories);
        
        Assert.True(testProjects.Length > 0, "At least one test project should exist to prevent empty 'dotnet test'");
        Assert.Contains("AutoDocOps.Tests.csproj", testProjects.Select(Path.GetFileName));
    }
    
    [Fact]
    public void CiGuard_SolutionExists()
    {
        // Verifica que existe el archivo de solución
        var currentDir = AppContext.BaseDirectory;
        var solutionRoot = FindSolutionRoot(currentDir);
        var solutionFiles = Directory.GetFiles(solutionRoot, "*.sln", SearchOption.TopDirectoryOnly);
        
        Assert.True(solutionFiles.Length > 0, "Solution file should exist");
        Assert.Contains("AutoDocOps.sln", solutionFiles.Select(Path.GetFileName));
    }
    
    private static string FindSolutionRoot(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            if (dir.GetFiles("*.sln").Length > 0) return dir.FullName;
            if (dir.GetDirectories(".git").Length > 0) return dir.FullName;
            if (dir.GetFiles("Directory.Build.props").Length > 0) return dir.FullName;
            dir = dir.Parent;
        }
        return startPath; // fallback
    }
}