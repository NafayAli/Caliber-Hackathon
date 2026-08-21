namespace Caliber.Api.Dtos.Common;

public sealed record PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

    public int TotalCount { get; init; }

    public int Offset { get; init; }

    public int Limit { get; init; }
}

public sealed record PagedQuery
{
    public int Offset { get; init; }

    public int Limit { get; init; } = 50;
}
