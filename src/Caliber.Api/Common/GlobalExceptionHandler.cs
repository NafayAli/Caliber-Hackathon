using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Caliber.Api.Common;

/// <summary>
/// Turns every unhandled exception into an RFC 9457 ProblemDetails response.
/// Stack traces never cross the wire outside Development; a traceId does, so a
/// user-reported failure can be found in the logs.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        IHostEnvironment environment,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _environment = environment;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = Describe(exception);

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled fault on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogInformation(
                "Request rejected on {Method} {Path}: {Reason}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                exception.Message);
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (_environment.IsDevelopment() && problem.Status >= StatusCodes.Status500InternalServerError)
        {
            problem.Extensions["exception"] = exception.ToString();
        }

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem,
        });
    }

    private ProblemDetails Describe(Exception exception)
    {
        switch (exception)
        {
            case ValidationException validation:
                var details = new ValidationProblemDetails(validation.Errors)
                {
                    Status = validation.StatusCode,
                    Title = validation.Title,
                };
                return details;

            case AppException app:
                return new ProblemDetails
                {
                    Status = app.StatusCode,
                    Title = app.Title,
                    Detail = app.Message,
                };

            // A lost update: someone else changed the row since it was read.
            case DbUpdateConcurrencyException:
                return new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Conflict",
                    Detail = "This record was changed by someone else while you were editing it. Reload and try again.",
                };

            default:
                return new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Something went wrong",
                    Detail = _environment.IsDevelopment()
                        ? exception.Message
                        : "An unexpected error occurred. Quote the trace id when reporting this.",
                };
        }
    }
}
