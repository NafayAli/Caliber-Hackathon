using System.Threading.RateLimiting;
using Caliber.Api.Abstractions;
using Caliber.Api.Common;
using Caliber.Api.Data;
using Caliber.Api.Security;
using Caliber.Api.Storage;
using Caliber.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;

namespace Caliber.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public const string CorsPolicyName = "caliber-spa";

    public static IServiceCollection AddCaliberServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EvidenceStorageOptions>(configuration.GetSection(EvidenceStorageOptions.SectionName));
        services.Configure<AvatarStorageOptions>(configuration.GetSection(AvatarStorageOptions.SectionName));
        services.AddScoped<ReadinessService>();
        services.AddScoped<ReportService>();
        services.AddScoped<CertificationService>();
        services.AddScoped<TrainingService>();
        services.AddScoped<SkillService>();
        services.AddScoped<RoleRequirementService>();
        services.AddScoped<EmployeeService>();
        services.AddScoped<EvidenceService>();
        services.AddScoped<AuthService>();
        services.AddScoped<SettingsService>();
        services.AddScoped<SkillAssignmentRequestService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<RenewalService>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<CurrentUser>();
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUser>());
        services.AddSingleton<IEvidenceStorage, LocalFileEvidenceStorage>();
        services.AddSingleton<LocalFileAvatarStorage>();

        services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);

        services.AddControllers(options => options.Filters.Add<ValidationFilter>())
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new System.Text.Json.Serialization.JsonStringEnumConverter());
            });

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Caliber API",
                Version = "v1",
                Description = "Workforce readiness: certifications, training, skills, and evidence.",
            });
        });

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                             ?? new[] { "http://localhost:5173" };

        services.AddCors(options => options.AddPolicy(CorsPolicyName, policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = AuthConstants.CookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

        services.AddAuthorization();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                await context.HttpContext.Response.WriteAsync(
                    "Too many requests. Try again later.",
                    cancellationToken);
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    Program.PartitionKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 300,
                        Window = TimeSpan.FromMinutes(1),
                    }));

            options.AddPolicy("uploads", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    Program.PartitionKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                    }));
        });

        return services;
    }

    public static IServiceCollection AddCaliberDb(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddDbContext<CaliberDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("Caliber"),
                sql => sql.EnableRetryOnFailure());

            if (environment.IsDevelopment())
            {
                options.EnableDetailedErrors();
            }
        });

        services.AddHealthChecks()
            .AddDbContextCheck<CaliberDbContext>("database");

        return services;
    }
}
