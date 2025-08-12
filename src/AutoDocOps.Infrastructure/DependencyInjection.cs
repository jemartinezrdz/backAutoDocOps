using AutoDocOps.Application.Authentication.Models;
using AutoDocOps.Application.Authentication.Services;
using AutoDocOps.Application.Common.Interfaces;
using AutoDocOps.Application.Common.Models;
using AutoDocOps.Domain.Interfaces;
using AutoDocOps.Infrastructure.Authentication;
using AutoDocOps.Infrastructure.Data;
using AutoDocOps.Infrastructure.HealthChecks;
using AutoDocOps.Infrastructure.Repositories;
using AutoDocOps.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using StackExchange.Redis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Polly;
using Polly.Extensions.Http;
using Microsoft.Extensions.Http;
using System.Net.Http;
using Stripe;
using AutoDocOps.Infrastructure.Monitoring;
using System.Threading.Channels;
using AutoDocOps.Domain.Entities;

namespace AutoDocOps.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configuration);
        // Add DbContext
        services.AddDbContext<AutoDocOpsDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("DefaultConnection is required but not configured. Please set the ConnectionStrings:DefaultConnection configuration value.");
            
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

        // Add Redis ConnectionMultiplexer for pattern-based operations
        services.AddSingleton<IConnectionMultiplexer>(provider =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        // Add cache service
        services.AddScoped<ICacheService, RedisCacheService>();

        // Resilient HttpClient for billing-related outbound calls (placeholder for future external integrations)
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => (int)msg.StatusCode == 429)
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)));
        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

        services.AddHttpClient("Billing")
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(5))
            .AddPolicyHandler(retryPolicy)
            .AddPolicyHandler(circuitBreakerPolicy);

    // Stripe client singleton registered via interface for easier testing
    services.AddSingleton<IStripeClient>(_ => new StripeClient(configuration["Stripe:SecretKey"] ?? string.Empty));

        // Add billing service
    services.AddScoped<IBillingService, Services.BillingService>();
    services.AddSingleton<ISecretSourceProvider, SecretSourceProvider>();
        services.AddSingleton(Channel.CreateBounded<BillingAuditLog>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropWrite
        }));
        services.AddScoped<IBillingAuditService, BillingAuditService>();
        services.AddHostedService<BillingAuditBackgroundService>();
    // Audit background processing channel + hosted service configured in BillingAuditService file

        // Add LLM client
                var useFakeLlm = Environment.GetEnvironmentVariable("USE_FAKE_LLM")?.ToLowerInvariant() == "true";
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

        // Add background services (conditionally based on environment/configuration)
        var enableDocumentationGeneration = configuration.GetValue<bool>("Features:EnableDocumentationGeneration", false);
        if (enableDocumentationGeneration)
        {
            services.AddHostedService<DocumentationGenerationService>();
        }

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
                    throw new InvalidOperationException("DefaultConnection is required for health checks but not configured."),
                name: "database")
            .AddRedis(configuration.GetConnectionString("Redis") ?? "localhost:6379",
                name: "redis_cache");

        return services;
    }
}

