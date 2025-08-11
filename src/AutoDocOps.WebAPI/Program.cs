using AutoDocOps.Application.Projects.Commands.CreateProject;
using AutoDocOps.Application.Common.Interfaces;
using AutoDocOps.Application.Common.Profiles;
using AutoDocOps.Application.Authentication.Models;
using AutoDocOps.Application.Common.Models;
using AutoDocOps.Infrastructure;
using AutoDocOps.WebAPI.Models;
using AutoDocOps.WebAPI.Controllers;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi.Models;
using Stripe;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateProjectCommand).Assembly));

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(ProjectProfile));

// Add Infrastructure (includes authentication)
builder.Services.AddInfrastructure(builder.Configuration);

// Strict validation of critical configuration (fail-fast)
builder.Services.AddOptions<JwtSettings>()
    .BindConfiguration(JwtSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<DbSettings>()
    .BindConfiguration(DbSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

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

// Add rate limiting for webhooks
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("stripe", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            }));
});

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

// Add rate limiting middleware
app.UseRateLimiter();

// Add session middleware
app.UseSession();

// Add authentication & authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Stripe webhook endpoint with security enhancements
app.MapPost("/billing/stripe-webhook", HandleStripeWebhookAsync)
    .RequireRateLimiting("stripe")
    .Accepts<string>("application/json")
    .WithName("StripeWebhook")
    .WithTags("Billing")
    .AllowAnonymous();

// Secure Stripe webhook handler
static async Task<IResult> HandleStripeWebhookAsync(
    HttpRequest request,
    ILogger<Program> logger,
    IConfiguration config,
    IBillingService billingService,
    CancellationToken cancellationToken = default)
{
    const int MaxBodyBytes = 256 * 1024; // 256 KB limit

    // Validate content type
    if (!request.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) ?? true)
    {
        return Results.BadRequest("Content-Type must be application/json");
    }

    // Read body with size limit using a pooled buffer to avoid large allocations.
    string json;
    var rentedBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(MaxBodyBytes);
    try
    {
        int totalRead = 0;
        int bytesRead;
        var bufferMemory = new Memory<byte>(rentedBuffer);

    while (totalRead < MaxBodyBytes && (bytesRead = await request.Body.ReadAsync(bufferMemory.Slice(totalRead), cancellationToken).ConfigureAwait(false)) > 0)
        {
            totalRead += bytesRead;
        }

        // Check if there's still more data in the request body, which means it's too large.
        if (totalRead == MaxBodyBytes)
        {
            if (await request.Body.ReadAsync(new byte[1], 0, 1, cancellationToken).ConfigureAwait(false) > 0)
            {
                return Results.BadRequest("Request body too large");
            }
        }

        json = Encoding.UTF8.GetString(rentedBuffer, 0, totalRead);
    }
    finally
    {
        System.Buffers.ArrayPool<byte>.Shared.Return(rentedBuffer);
    }
    
    // Validate Stripe signature
    var stripeSignature = request.Headers["Stripe-Signature"].FirstOrDefault();
    if (string.IsNullOrEmpty(stripeSignature))
    {
        return Results.BadRequest("Missing Stripe signature");
    }

    var webhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET")
        ?? config["Stripe:WebhookSecret"];
    
    if (string.IsNullOrWhiteSpace(webhookSecret))
    {
        logger.StripeWebhookSecretNotConfigured();
        return Results.Problem("Webhook secret not configured", statusCode: 500);
    }

    Event stripeEvent;
    try
    {
        stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret, throwOnApiVersionMismatch: false);
    }
    catch (Exception ex)
    {
        logger.InvalidStripeWebhookSignature(ex);
        return Results.BadRequest("Invalid signature");
    }

    try
    {
    await billingService.HandleAsync(stripeEvent, cancellationToken).ConfigureAwait(false);
        return Results.Ok();
    }
    catch (Exception ex)
    {
        logger.ErrorProcessingStripeWebhook(stripeEvent.Type, ex);
        return Results.StatusCode(500);
    }
}

// Chat streaming endpoint
app.MapPost("/chat/stream", async (ChatRequest request, ILlmClient llmClient, HttpContext context, ILogger<Program> logger) =>
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
        await foreach (var chunk in llmClient.StreamChatAsync(request.Query, context.RequestAborted).ConfigureAwait(false))
        {
            await context.Response.WriteAsync(chunk, context.RequestAborted).ConfigureAwait(false);
            await context.Response.Body.FlushAsync(context.RequestAborted).ConfigureAwait(false);
        }
    }
    catch (OperationCanceledException)
    {
        // Client disconnected, this is normal
    }
    catch (Exception ex)
    {
        logger.ChatStreamingError(ex.Message, ex);
    await context.Response.WriteAsync($"Error: {ex.Message}", context.RequestAborted).ConfigureAwait(false);
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

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.ApiStarting(app.Environment.EnvironmentName);
logger.AvailableEndpoints();

await app.RunAsync().ConfigureAwait(false);
