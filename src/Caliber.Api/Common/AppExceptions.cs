namespace Caliber.Api.Common;

/// <summary>
/// Base for exceptions that carry a deliberate HTTP meaning. Anything not derived
/// from this is an unexpected fault and becomes a 500 with no detail leaked.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message)
        : base(message)
    {
    }

    public abstract int StatusCode { get; }

    public abstract string Title { get; }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string what, object key)
        : base($"{what} '{key}' was not found.")
    {
    }

    public override int StatusCode => StatusCodes.Status404NotFound;

    public override string Title => "Not found";
}

public sealed class ConflictException : AppException
{
    public ConflictException(string message)
        : base(message)
    {
    }

    public override int StatusCode => StatusCodes.Status409Conflict;

    public override string Title => "Conflict";
}

/// <summary>The caller is known but is not allowed to touch this particular resource.</summary>
public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message = "You do not have access to this resource.")
        : base(message)
    {
    }

    public override int StatusCode => StatusCodes.Status403Forbidden;

    public override string Title => "Forbidden";
}

public sealed class BadRequestException : AppException
{
    public BadRequestException(string message)
        : base(message)
    {
    }

    public override int StatusCode => StatusCodes.Status400BadRequest;

    public override string Title => "Invalid request";
}

/// <summary>Field-level validation failures, surfaced so the client can attach them to inputs.</summary>
public sealed class ValidationException : AppException
{
    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public IDictionary<string, string[]> Errors { get; }

    public override int StatusCode => StatusCodes.Status400BadRequest;

    public override string Title => "Validation failed";
}

public sealed class PayloadTooLargeException : AppException
{
    public PayloadTooLargeException(long maxBytes)
        : base($"The uploaded file exceeds the maximum allowed size of {maxBytes:N0} bytes.")
    {
    }

    public override int StatusCode => StatusCodes.Status413PayloadTooLarge;

    public override string Title => "Payload too large";
}

public sealed class UnsupportedMediaTypeException : AppException
{
    public UnsupportedMediaTypeException(string message = "The uploaded file type is not supported.")
        : base(message)
    {
    }

    public override int StatusCode => StatusCodes.Status415UnsupportedMediaType;

    public override string Title => "Unsupported media type";
}
