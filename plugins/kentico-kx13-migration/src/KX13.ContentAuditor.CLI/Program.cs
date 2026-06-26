using KX13.ContentAuditor.CLI;
using KX13.ContentAuditor.Application;
using KX13.ContentAuditor.DataAccess;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

AuditCliOptions options = AuditCliOptionsParser.Parse(args);

if (options.Errors.Count > 0)
{
    foreach (string error in options.Errors)
    {
        Console.Error.WriteLine($"Error: {error}");
    }

    Console.Error.WriteLine("Run with --help for usage information.");
    return;
}

if (options.ShowHelp)
{
    AuditCliUsage.WriteTo(Console.Out);
    return;
}

var builder = CreateBuilder(args);

builder.Services.AddDataAccess(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddTransient<AuditCliRunner>();

var host = builder.Build();

var runner = host.Services.GetRequiredService<AuditCliRunner>();
await runner.ExecuteAsync(options);

static HostApplicationBuilder CreateBuilder(string[] args)
{
    string contentRoot = AppContext.BaseDirectory;

    var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
    {
        Args = args,
        ContentRootPath = contentRoot
    });

    builder.Configuration.Sources.Clear();
    builder.Configuration
        .SetBasePath(contentRoot)
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
        .AddJsonFile("appsettings.development.json", optional: true, reloadOnChange: false)
        .AddEnvironmentVariables()
        .AddCommandLine(args);

    return builder;
}


