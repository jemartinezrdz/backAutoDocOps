using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using AutoDocOps.Application.Authentication.Models;
using AutoDocOps.Application.Common.Models;
using Xunit;

namespace AutoDocOps.Tests.Configuration;

public class MissingEnvTests
{
    [Fact]
    public void JwtSettings_ShouldFailValidation_WhenSecretKeyIsMissing()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Issuer"] = "AutoDocOps",
                ["JwtSettings:Audience"] = "AutoDocOps.API"
                // SecretKey intentionally missing
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<JwtSettings>()
            .BindConfiguration(JwtSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(() =>
            serviceProvider.GetRequiredService<IOptions<JwtSettings>>().Value);
        
        Assert.Contains("JWT SecretKey is required", exception.Message);
    }

    [Fact]
    public void JwtSettings_ShouldFailValidation_WhenSecretKeyIsTooShort()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "short", // Less than 32 characters
                ["JwtSettings:Issuer"] = "AutoDocOps",
                ["JwtSettings:Audience"] = "AutoDocOps.API"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<JwtSettings>()
            .BindConfiguration(JwtSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(() =>
            serviceProvider.GetRequiredService<IOptions<JwtSettings>>().Value);
        
        Assert.Contains("JWT SecretKey must be at least 32 characters", exception.Message);
    }

    [Fact]
    public void DbSettings_ShouldFailValidation_WhenConnectionStringIsMissing()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"] = "localhost:6379"
                // DefaultConnection intentionally missing
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<DbSettings>()
            .BindConfiguration(DbSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(() =>
            serviceProvider.GetRequiredService<IOptions<DbSettings>>().Value);
        
        Assert.Contains("Database connection string is required", exception.Message);
    }

    [Fact]
    public void AllSettings_ShouldPassValidation_WhenProperlyConfigured()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "this_is_a_secure_jwt_secret_key_with_32_plus_characters",
                ["JwtSettings:Issuer"] = "AutoDocOps",
                ["JwtSettings:Audience"] = "AutoDocOps.API",
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=autodocops;Username=postgres;Password=postgres",
                ["ConnectionStrings:Redis"] = "localhost:6379"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<JwtSettings>()
            .BindConfiguration(JwtSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        services.AddOptions<DbSettings>()
            .BindConfiguration(DbSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert - Should not throw
        var jwtSettings = serviceProvider.GetRequiredService<IOptions<JwtSettings>>().Value;
        var dbSettings = serviceProvider.GetRequiredService<IOptions<DbSettings>>().Value;

        Assert.NotNull(jwtSettings);
        Assert.NotNull(dbSettings);
        Assert.Equal("this_is_a_secure_jwt_secret_key_with_32_plus_characters", jwtSettings.SecretKey);
        Assert.Equal("Host=localhost;Database=autodocops;Username=postgres;Password=postgres", dbSettings.DefaultConnection);
    }
}
