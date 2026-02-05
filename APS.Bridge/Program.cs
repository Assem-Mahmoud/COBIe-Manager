using ACG.Aps.Core.Helpers;
using ACG.Aps.Core.Services;
using APS.Bridge.Controllers;
using APS.Bridge.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on localhost:5000
builder.WebHost.UseUrls("http://localhost:5000");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();

// Register ACG.APS.Core services
builder.Services.AddSingleton<TokenStorage>();
builder.Services.AddSingleton<ApsAuthService>();
builder.Services.AddSingleton<ApsSessionManager>();

// Register bridge models
builder.Services.AddSingleton<BridgeAuthStatus>();

// Register controllers
builder.Services.AddScoped<AuthController>();
builder.Services.AddScoped<ParametersController>();

// Add CORS policy for local requests
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalOrigins", policy =>
    {
        policy.WithOrigins("http://localhost:5000", "http://127.0.0.1:5000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add logging
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var app = builder.Build();

// Configure CORS
app.UseCors("LocalOrigins");

// Map controllers
app.MapControllers();

// Graceful shutdown handling
var cts = new CancellationTokenSource();

// Handle shutdown signals
AppDomain.CurrentDomain.ProcessExit += (s, e) =>
{
    Console.WriteLine("APS Bridge shutting down...");
    cts.Cancel();
};

Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    Console.WriteLine("APS Bridge shutting down...");
    cts.Cancel();
};

app.Lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("APS Bridge stopping...");
});

// Start the bridge
Console.WriteLine("APS Bridge starting on http://localhost:5000");
Console.WriteLine("Press Ctrl+C to stop the bridge");

try
{
    await app.RunAsync(cts.Token);
}
catch (OperationCanceledException)
{
    // Expected during shutdown
}
finally
{
    Console.WriteLine("APS Bridge stopped");
}
