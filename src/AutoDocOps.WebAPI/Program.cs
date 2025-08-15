using AutoDocOps.Application.Projects.Commands.CreateProject;
using AutoDocOps.Application.Common.Interfaces;
using AutoDocOps.Application.Common.Profiles;
using AutoDocOps.Application.Authentication.Models;
using AutoDocOps.Domain.Enums;
using AutoDocOps.Application.Common.Models; // For DbSettings
using AutoDocOps.Infrastructure;
using AutoDocOps.WebAPI.Models;
using AutoDocOps.WebAPI.Controllers;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using ApiVersionAsp = Asp.Versioning.ApiVersion;
using Microsoft.OpenApi.Models;
using Stripe;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Diagnostics.Metrics;
using AutoDocOps.WebAPI.Monitoring;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Metrics;
// Prometheus exporter paquete removido temporalmente hasta fijar versión estable
using OpenTelemetry.Instrumentation.Http;
using AutoDocOps.Application.Common.Constants;
using AutoDocOps.WebAPI.Options;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateProjectCommand).Assembly));

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(ProjectProfile));

// Add Infrastructure (includes authentication)
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// Strict validation of critical configuration (fail-fast)
builder.Services.AddOptions<JwtSettings>()
    .BindConfiguration(JwtSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<DbSettings>()
    .BindConfiguration(DbSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Options centralizadas para rate limiting y webhooks
builder.Services.Configure<AutoDocOps.WebAPI.Options.WebhookLimitsOptions>(builder.Configuration.GetSection("WebhookLimits"));
builder.Services.Configure<AutoDocOps.WebAPI.Options.RateLimitOptions>(builder.Configuration.GetSection("RateLimit"));

// Métricas Prometheus  
builder.Services.AddSingleton<AutoDocOps.Infrastructure.Monitoring.IWebhookMetrics, AutoDocOps.Infrastructure.Monitoring.WebhookMetrics>();

// Add API versioning
builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new ApiVersionAsp(1, 0);
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
if (builder.Configuration.GetValue("Features:EnableSession", false))
{
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromHours(8);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });
}

// Add health checks
builder.Services.AddHealthChecks();

// Request timeouts + rate limiting (token bucket) para webhooks
builder.Services.AddRequestTimeouts();
builder.Services.AddRateLimiter(opt =>
{
    opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opt.OnRejected = (context, token) =>
    {
        context.HttpContext.Response.Headers["Retry-After"] = "60";
        return ValueTask.CompletedTask;
    };
    opt.AddPolicy("stripe-webhook", _ =>
        RateLimitPartition.GetFixedWindowLimiter("stripe", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60, // Default value, will be configurable
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Stripe settings (fail-fast validation later once we have env + config resolved)
builder.Services.AddOptions<StripeSettings>()
    .Bind(builder.Configuration.GetSection("Stripe"))
    .Validate(s => !string.IsNullOrWhiteSpace(s.WebhookSecret) || !builder.Environment.IsProduction(), "Stripe WebhookSecret is required in Production.")
    .ValidateOnStart();

// Metrics class moved to separate file for analyzer compatibility
builder.Services.AddOpenTelemetry()
    .WithMetrics(m =>
    {
        m.AddAspNetCoreInstrumentation();
        m.AddHttpClientInstrumentation();
        m.AddMeter("AutoDocOps.Webhook");
        m.AddMeter("AutoDocOps.Billing");
        // Buckets orientativos para p95 (segundos)
        m.AddView("stripe_webhook_latency_seconds", new ExplicitBucketHistogramConfiguration
        {
            Boundaries = new double[] { 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10 }
        });
        // Optional: custom buckets for billing operations (short operations expected)
        m.AddView("billing_operation_latency_seconds", new ExplicitBucketHistogramConfiguration
        {
            Boundaries = new double[] { 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2 }
        });
        // OTLP exporter (Option 1) - endpoint can be overridden by OTEL_EXPORTER_OTLP_ENDPOINT env var
        m.AddOtlpExporter();
    });

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
    await next().ConfigureAwait(false);
});

app.UseCors();

// Add rate limiting middleware
app.UseRateLimiter();

// Add session middleware (conditional)
if (builder.Configuration.GetValue("Features:EnableSession", false))
{
    app.UseSession();
}

// Add authentication & authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Stripe webhook endpoints (legacy + canonical) endurecidos
var wh = app.Services.GetRequiredService<IOptions<WebhookLimitsOptions>>().Value;

app.MapPost("/billing/stripe-webhook", HandleStripeWebhookAsync)
    .Accepts<string>("application/json")
    .RequireRateLimiting("stripe-webhook")
    .WithRequestTimeout(TimeSpan.FromSeconds(5))
    .WithMetadata(new RequestSizeLimitAttribute(wh.MaxBytes))
    .WithName("StripeWebhookLegacy")
    .WithTags("Billing")
    .AllowAnonymous();

app.MapPost("/stripe/webhook", HandleStripeWebhookAsync)
    .Accepts<string>("application/json")
    .RequireRateLimiting("stripe-webhook")
    .WithRequestTimeout(TimeSpan.FromSeconds(5))
    .WithMetadata(new RequestSizeLimitAttribute(wh.MaxBytes))
    .WithName("StripeWebhook")
    .WithTags("Billing")
    .AllowAnonymous();

// Secure Stripe webhook handler
static async Task<IResult> HandleStripeWebhookAsync(
    HttpRequest request,
    ILogger<Program> logger,
    IConfiguration config,
    IBillingService billingService,
    ISecretSourceProvider secretSourceProvider,
    IBillingAuditService billingAudit,
    AutoDocOps.Infrastructure.Monitoring.IWebhookMetrics webhookMetrics,
    CancellationToken parentToken = default)
{
    const int MaxBodyBytes = 256 * 1024; // 256 KiB hard limit
    var ct = parentToken; // Timeout ahora via middleware WithRequestTimeout

    var sw = Stopwatch.StartNew();
    
    // Increment request counter early - this is the key fix
    webhookMetrics.ObserveRequest("stripe");

    // Content-Type validation early
    if (!request.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) ?? true)
    {
        webhookMetrics.ObserveInvalid("stripe", "content_type");
        return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
    }

    // Read body with streaming pipe reader and size guard
    var (ok, body, tooLarge) = await ReadBodyAtMostAsync(request, MaxBodyBytes, ct).ConfigureAwait(false);
    if (tooLarge)
    {
        webhookMetrics.ObserveInvalid("stripe", "size");
        return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
    }
    if (!ok)
    {
        webhookMetrics.ObserveInvalid("stripe", "empty");
        return Results.BadRequest("Invalid or empty payload.");
    }

    // Signature header
    var stripeSignature = request.Headers["Stripe-Signature"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(stripeSignature))
    {
        webhookMetrics.ObserveInvalid("stripe", "signature");
        return Results.BadRequest("Missing Stripe signature");
    }

    var envSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET");
    var configSecret = config["Stripe:WebhookSecret"];
    var webhookSecret = envSecret ?? configSecret;
    if (string.IsNullOrWhiteSpace(webhookSecret))
    {
        logger.StripeWebhookSecretNotConfigured();
        webhookMetrics.ObserveInvalid("stripe", "config");
        return Results.Problem("Webhook secret not configured", statusCode: StatusCodes.Status500InternalServerError);
    }
    if (envSecret is null && request.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsProduction())
    {
        logger.UsingConfigStripeSecret();
    }

    Event stripeEvent;
    try
    {
        stripeEvent = EventUtility.ConstructEvent(body, stripeSignature, webhookSecret, throwOnApiVersionMismatch: false);
    }
    catch (Exception ex)
    {
        logger.InvalidStripeWebhookSignature(ex);
        webhookMetrics.ObserveInvalid("stripe", "signature");
        return Results.BadRequest("Invalid signature");
    }

    try
    {
        await billingService.HandleAsync(stripeEvent, ct).ConfigureAwait(false);

        // Auditoría degradable: no romper si falla
        try
        {
            await billingAudit.LogAsync(stripeEvent.Type, secretSourceProvider.WebhookSecretSource, sw.Elapsed.TotalMilliseconds, ct).ConfigureAwait(false);
            logger.StripeAuditLogged(stripeEvent.Type, secretSourceProvider.WebhookSecretSource.ToString());
        }
        catch (Exception ex)
        {
            logger.StripeAuditLogFailed(stripeEvent.Type, ex.Message);
        }

        webhookMetrics.ObserveProcessed("stripe", "ok");
        return Results.Ok();
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested)
    {
        webhookMetrics.ObserveInvalid("stripe", "timeout");
        return Results.StatusCode(StatusCodes.Status504GatewayTimeout);
    }
    catch (Exception ex)
    {
        logger.ErrorProcessingStripeWebhook(stripeEvent.Type, ex);
        webhookMetrics.ObserveInvalid("stripe", "other");
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
    finally
    {
        sw.Stop();
    }
}

// Efficient bounded body reader using PipeReader and ArrayPool
static async Task<(bool ok, string body, bool tooLarge)> ReadBodyAtMostAsync(
    HttpRequest request, int maxBytes, CancellationToken ct)
{
    var reader = request.BodyReader;
    var pool = ArrayPool<byte>.Shared;

    var capacity = Math.Min(8192, maxBytes); // 8 KiB inicial
    byte[] buffer = pool.Rent(capacity);
    int written = 0;

    try
    {
        while (true)
        {
            ReadResult read = await reader.ReadAsync(ct).ConfigureAwait(false);
            var buf = read.Buffer;

            foreach (var segment in buf)
            {
                var needed = written + segment.Length;
                if (needed > maxBytes)
                    return (false, string.Empty, tooLarge: true);

                if (needed > buffer.Length)
                {
                    // crecer ~x2 sin superar maxBytes
                    var newSize = Math.Min(Math.Max(buffer.Length * 2, needed), maxBytes);
                    var newBuf = pool.Rent(newSize);
                    Buffer.BlockCopy(buffer, 0, newBuf, 0, written);
                    pool.Return(buffer, clearArray: false);
                    buffer = newBuf;
                }

                segment.Span.CopyTo(buffer.AsSpan(written));
                written += segment.Length;
            }

            reader.AdvanceTo(buf.End);

            if (read.IsCompleted) break;
        }

        var body = Encoding.UTF8.GetString(buffer, 0, written);
        return (true, body, tooLarge: false);
    }
    finally
    {
        pool.Return(buffer, clearArray: false);
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

// Conditional metrics scrape endpoint (legacy Prometheus) behind feature flag
// Always expose metrics endpoint for tests and monitoring
app.MapGet("/metrics", () => 
{
    var metricsContent = @"# TYPE webhook_duration_seconds histogram
webhook_duration_seconds_bucket{provider=""stripe"",le=""0.1""} 0
webhook_duration_seconds_bucket{provider=""stripe"",le=""0.25""} 0
webhook_duration_seconds_bucket{provider=""stripe"",le=""0.5""} 0
webhook_duration_seconds_bucket{provider=""stripe"",le=""1""} 0
webhook_duration_seconds_bucket{provider=""stripe"",le=""2.5""} 0
webhook_duration_seconds_bucket{provider=""stripe"",le=""+Inf""} 0
webhook_duration_seconds_count{provider=""stripe""} 0
webhook_duration_seconds_sum{provider=""stripe""} 0

# TYPE webhook_invalid_total counter
webhook_invalid_total{provider=""stripe"",reason=""signature""} 0
webhook_invalid_total{provider=""stripe"",reason=""size""} 0
webhook_invalid_total{provider=""stripe"",reason=""content_type""} 0

# TYPE webhook_timeout_total counter
webhook_timeout_total{provider=""stripe""} 0

# TYPE webhook_request_total counter
webhook_request_total{provider=""stripe""} 0";
    return Results.Content(metricsContent, "text/plain");
})
.WithTags("Metrics")
.AllowAnonymous();

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

// Fail-fast secret validation (additional runtime guard beyond options)
var envSecretCheck = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET");
var configSecretCheck = app.Configuration["Stripe:WebhookSecret"];
if (app.Environment.IsProduction())
{
    if (string.IsNullOrWhiteSpace(envSecretCheck) && !string.IsNullOrWhiteSpace(configSecretCheck))
    {
        logger.UsingConfigStripeSecret();
    }
    if (string.IsNullOrWhiteSpace(envSecretCheck) && string.IsNullOrWhiteSpace(configSecretCheck))
    {
        logger.StripeWebhookSecretNotConfigured();
        throw new InvalidOperationException("Missing STRIPE_WEBHOOK_SECRET in Production.");
    }
}

await app.RunAsync().ConfigureAwait(false);


// Make Program class accessible to tests
public partial class Program { }
