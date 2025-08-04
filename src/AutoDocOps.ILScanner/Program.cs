using AutoDocOps.ILScanner.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container
builder.Services.AddGrpc();
builder.Services.AddScoped<RoslynAnalyzer>();
builder.Services.AddScoped<SqlAnalyzer>();

// Configure Kestrel for gRPC
builder.WebHost.ConfigureKestrel(options =>
{
    // Listen on all interfaces for gRPC
    options.ListenAnyIP(5000, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.MapGrpcService<ILScannerGrpcService>();

// Health check endpoint
app.MapGet("/health", () => new { Status = "Healthy", Timestamp = DateTime.UtcNow });

// gRPC reflection for development
if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

Console.WriteLine("AutoDocOps IL Scanner gRPC service starting...");
Console.WriteLine("Listening on: http://0.0.0.0:5000");

app.Run();
