using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using SharenoteGainsight.Worker;
using SharenoteGainsight.Worker.DI;
using System;
using System.IO;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Load configuration
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// Resolve Logs path BEFORE NLog loads
var logDir = builder.Configuration["Paths:Logs"];
if (string.IsNullOrWhiteSpace(logDir))
{
    logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
}
Directory.CreateDirectory(logDir);

// Set NLog variable BEFORE AddNLog()
LogManager.Configuration ??= new NLog.Config.LoggingConfiguration();
LogManager.Configuration.Variables["logDir"] = logDir;

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddNLog();

// Dependency Injection
builder.Services.AddMyServices();

// Register Worker
builder.Services.AddHostedService<Worker>();

// Build & Run
var app = builder.Build();
app.Run();
