using AutoDocOps.Application.Authentication.Models;
using AutoDocOps.Application.Authentication.Services;
using AutoDocOps.Application.Common.Interfaces;
using AutoDocOps.Domain.Interfaces;
using AutoDocOps.Infrastructure.Authentication;
using AutoDocOps.Infrastructure.Data;
using AutoDocOps.Infrastructure.HealthChecks;
using AutoDocOps.Infrastructure.Repositories;
using AutoDocOps.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

        // Add Redis Cache
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "AutoDocOps";
        });

        // Add cache service
        services.AddScoped<ICacheService, RedisCacheService>();

        // Add billing service
        services.AddScoped<IBillingService, BillingService>();

        // Add LLM client
        var useFakeLlm = Environment.GetEnvironmentVariable("USE_FAKE_LLM")?.ToLower() == "true";
        if (useFakeLlm)
        {
            services.AddScoped<ILlmClient, FakeLlmClient>();
        }
        else
        {
            services.AddScoped<ILlmClient, OpenAILlmClient>();
        }

        // Add repositories
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ISpecRepository, SpecRepository>();
        services.AddScoped<IPassportRepository, PassportRepository>();

        // Configure options
        services.Configure<DocumentationGenerationOptions>(
            configuration.GetSection(DocumentationGenerationOptions.SectionName));

        // Add background services
        // services.AddHostedService<DocumentationGenerationService>(); // Temporarily disabled for testing

        // Configure JWT settings
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        // Add authentication services
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Add authorization handlers
        services.AddScoped<IAuthorizationHandler, OrganizationAuthorizationHandler>();

        // Configure JWT authentication
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();
        if (jwtSettings != null && !string.IsNullOrEmpty(jwtSettings.SecretKey))
        {
            var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);
            
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // Only disable HTTPS metadata in development
                var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                options.RequireHttpsMetadata = env != "Development";
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("OrganizationAccess", policy =>
                    policy.Requirements.Add(new OrganizationRequirement()));
                
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireRole("Admin"));
                
                options.AddPolicy("DeveloperOrAdmin", policy =>
                    policy.RequireRole("Developer", "Admin"));
            });
        }

        // Add Health Checks
        services.AddHealthChecks()
            .AddCheck<DocumentationServiceHealthCheck>("documentation_service")
            .AddCheck<CacheHealthCheck>("cache_service") 
            .AddCheck<LlmHealthCheck>("llm_service")
            .AddNpgSql(
                connectionString: configuration.GetConnectionString("DefaultConnection") ?? 
                    "Host=localhost;Database=autodocops;Username=postgres;Password=postgres",
                name: "database")
            .AddRedis(
                connectionString: configuration.GetConnectionString("Redis") ?? "localhost:6379",
                name: "redis_cache");

        return services;
    }
}

