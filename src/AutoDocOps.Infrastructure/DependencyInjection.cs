using AutoDocOps.Domain.Interfaces;
using AutoDocOps.Infrastructure.Data;
using AutoDocOps.Infrastructure.Repositories;
using AutoDocOps.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AutoDocOps.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<AutoDocOpsDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? "Host=localhost;Database=autodocops;Username=postgres;Password=postgres";
            
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(AutoDocOpsDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });

            // Enable sensitive data logging in development
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // Add repositories
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ISpecRepository, SpecRepository>();
        services.AddScoped<IPassportRepository, PassportRepository>();

        // Add background services
        services.AddHostedService<DocumentationGenerationService>();

        return services;
    }
}

