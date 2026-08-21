using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using ValidationException = Caliber.Api.Common.ValidationException;

namespace Caliber.Api.Common;

/// <summary>
/// Runs any registered FluentValidation validator against each action argument.
/// Failures are raised as <see cref="ValidationException"/> so they travel the same
/// path as every other error and come back as a ValidationProblemDetails with
/// field-level messages the client can attach to inputs.
/// </summary>
public sealed class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _services;

    public ValidationFilter(IServiceProvider services)
    {
        _services = services;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var failures = new Dictionary<string, List<string>>();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (_services.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            foreach (var error in result.Errors)
            {
                if (!failures.TryGetValue(error.PropertyName, out var messages))
                {
                    messages = new List<string>();
                    failures[error.PropertyName] = messages;
                }

                messages.Add(error.ErrorMessage);
            }
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()));
        }

        await next();
    }
}
