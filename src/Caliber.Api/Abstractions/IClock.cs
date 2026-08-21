namespace Caliber.Api.Abstractions;

/// <summary>
/// Injected rather than calling <c>DateTime.Now</c> inline, so seeded demo data can
/// be staged relative to a single consistent "today" and readiness calculations stay
/// testable.
/// </summary>
public interface IClock
{
    DateTimeOffset Now { get; }

    DateOnly Today { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;

    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}
