using Caliber.Api.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Caliber.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseCaliberPipeline(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseSerilogRequestLogging();

        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["Referrer-Policy"] = "no-referrer";
            headers["X-Frame-Options"] = "DENY";
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            await next();
        });

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseResponseCompression();
        app.UseCors(ServiceCollectionExtensions.CorsPolicyName);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<Security.PersonaMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Caliber API v1"));
        }

        return app;
    }

    public static async Task MigrateAndSeedAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CaliberDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        if (app.Environment.IsDevelopment())
        {
            var hasEmployees = await db.Employees.AnyAsync(cancellationToken);
            var hasCanonicalDemoAdmin = await db.UserAccounts
                .AnyAsync(u => u.Email == SeedData.CanonicalAdminEmail, cancellationToken);

            if (hasEmployees && !hasCanonicalDemoAdmin)
            {
                Log.Warning(
                    "Stale demo database detected (missing {Email}). Recreating database with current seed data.",
                    SeedData.CanonicalAdminEmail);
                await db.Database.EnsureDeletedAsync(cancellationToken);
                await db.Database.MigrateAsync(cancellationToken);
            }
        }

        await SeedData.EnsureSeededAsync(db, cancellationToken);
    }
}
