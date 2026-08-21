using Caliber.Api.Extensions;
using Caliber.Api.Security;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            Path.Combine(builder.Environment.ContentRootPath, "logs", "caliber-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7));

    builder.Services.AddCaliberServices(builder.Configuration);
    builder.Services.AddCaliberDb(builder.Configuration, builder.Environment);

    var app = builder.Build();

    app.UseCaliberPipeline();

    app.MapControllers();
    app.MapHealthChecks("/health");

    await app.MigrateAndSeedAsync();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Caliber API terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>Exposed so integration tests and the validator scan can reference the entry assembly.</summary>
public partial class Program
{
    public static string PartitionKey(HttpContext context)
    {
        var employeeId = context.User.FindFirst(AuthClaimTypes.EmployeeId)?.Value;
        if (!string.IsNullOrEmpty(employeeId))
        {
            return $"user:{employeeId}";
        }

        return context.Connection.RemoteIpAddress?.ToString() is { Length: > 0 } ip
            ? $"ip:{ip}"
            : "ip:unknown";
    }
}
