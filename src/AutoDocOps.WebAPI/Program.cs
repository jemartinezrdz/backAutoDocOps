using AutoDocOps.Application.Projects.Commands.CreateProject;
using AutoDocOps.Application.Common.Interfaces;
using AutoDocOps.Application.Common.Profiles;
using AutoDocOps.Infrastructure;
using AutoDocOps.WebAPI.Models;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi.Models;
using Stripe;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateProjectCommand).Assembly));

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(ProjectProfile));

// Add Infrastructure (includes authentication)
builder.Services.AddInfrastructure(builder.Configuration);

// Add API versioning
builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"),
        new QueryStringApiVersionReader("version")
    );
});

builder.Services.AddVersionedApiExplorer(setup =>
{
    setup.GroupNameFormat = "'v'VVV";
    setup.SubstituteApiVersionInUrl = true;
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AutoDocOps API",
        Version = "v1",
        Description = "Backend API robusta para AutoDocOps - Generación automática de documentación",
        Contact = new OpenApiContact
        {
            Name = "AutoDocOps Team",
            Email = "support@autodocops.com"
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add security definition for JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add distributed sessions
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Add health checks
builder.Services.AddHealthChecks();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AutoDocOps API v1");
        c.RoutePrefix = "swagger";
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
        c.EnableValidator();
    });
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseCors();

// Add session middleware
app.UseSession();

// Add authentication & authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Stripe webhook endpoint
app.MapPost("/billing/stripe-webhook", async (HttpRequest request, IBillingService billingService, IConfiguration config) =>
{
    try
    {
        var json = await new StreamReader(request.Body).ReadToEndAsync();
        var stripeSignature = request.Headers["Stripe-Signature"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(stripeSignature))
        {
            return Results.BadRequest("Missing Stripe signature");
        }

        var webhookSecret = config["Stripe:WebhookSecret"] 
            ?? Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET") 
            ?? throw new InvalidOperationException("STRIPE_WEBHOOK_SECRET is not configured");

        var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);
        
        await billingService.HandleAsync(stripeEvent);
        
        return Results.Ok();
    }
    catch (StripeException ex)
    {
        Console.WriteLine($"Stripe webhook error: {ex.Message}");
        return Results.BadRequest($"Stripe error: {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Webhook error: {ex.Message}");
        return Results.StatusCode(500);
    }
}).WithTags("Billing").AllowAnonymous();

// Chat streaming endpoint
app.MapPost("/chat/stream", async (ChatRequest request, ILlmClient llmClient, HttpContext context) =>
{
    if (string.IsNullOrWhiteSpace(request.Query))
    {
        return Results.BadRequest("Query is required");
    }

    context.Response.Headers.ContentType = "text/plain; charset=utf-8";
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.Headers.Connection = "keep-alive";
    
    try
    {
        await foreach (var chunk in llmClient.StreamChatAsync(request.Query, context.RequestAborted))
        {
            await context.Response.WriteAsync(chunk, context.RequestAborted);
            await context.Response.Body.FlushAsync(context.RequestAborted);
        }
    }
    catch (OperationCanceledException)
    {
        // Client disconnected, this is normal
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Chat streaming error: {ex.Message}");
        await context.Response.WriteAsync($"Error: {ex.Message}", context.RequestAborted);
    }
    
    return Results.Empty;
}).WithTags("Chat").RequireAuthorization();

app.MapControllers();

// Health check endpoints
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // Only basic liveness check
});

app.MapHealthChecks("/health/ready"); // Full readiness check

// API info endpoint
app.MapGet("/", () => new
{
    Service = "AutoDocOps API",
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    Timestamp = DateTime.UtcNow,
    Documentation = "/swagger"
}).WithTags("Info");

// Add health check endpoints
app.MapHealthChecks("/health");

Console.WriteLine("AutoDocOps API starting...");
Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
Console.WriteLine("Swagger UI available at: /swagger");
Console.WriteLine("Health checks available at: /health");

await app.RunAsync();
