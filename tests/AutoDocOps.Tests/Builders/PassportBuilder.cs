using AutoDocOps.Domain.Entities;

namespace AutoDocOps.Tests.Builders;

public class PassportBuilder
{
    private readonly Passport _passport = new()
    {
        Id = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        Version = "1.0.0",
        Format = "markdown",
        Status = PassportStatus.Generating,
        GeneratedBy = Guid.NewGuid(),
        GeneratedAt = DateTime.UtcNow,
        DocumentationContent = "Sample content",
        SizeInBytes = 128
    };

    public PassportBuilder ForProject(Guid projectId) { _passport.ProjectId = projectId; return this; }
    public PassportBuilder WithVersion(string version) { ArgumentNullException.ThrowIfNull(version); _passport.Version = version; return this; }
    public PassportBuilder WithFormat(string format) { ArgumentNullException.ThrowIfNull(format); _passport.Format = format; return this; }
    public PassportBuilder Completed() { _passport.Status = PassportStatus.Completed; _passport.CompletedAt = DateTime.UtcNow; return this; }
    public PassportBuilder WithGenerator(Guid userId) { _passport.GeneratedBy = userId; return this; }
    public PassportBuilder WithContent(string content) { ArgumentNullException.ThrowIfNull(content); _passport.DocumentationContent = content; _passport.SizeInBytes = content.Length; return this; }

    public Passport Build() => _passport;
}
