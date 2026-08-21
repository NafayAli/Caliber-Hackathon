using Caliber.Api.Abstractions;
using FluentValidation;

namespace Caliber.Api.Validators;

internal static class CaliberValidationExtensions
{
    public static IRuleBuilderOptions<T, byte[]> ValidRowVersion<T>(this IRuleBuilder<T, byte[]> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .WithMessage("RowVersion is required for optimistic concurrency.");

    public static IRuleBuilderOptions<T, DateOnly> NotInFuture<T>(
        this IRuleBuilder<T, DateOnly> ruleBuilder,
        IClock clock) =>
        ruleBuilder
            .Must(date => date <= clock.Today)
            .WithMessage("Date cannot be in the future.");

    public static IRuleBuilderOptions<T, DateOnly?> NotInFutureWhenSet<T>(
        this IRuleBuilder<T, DateOnly?> ruleBuilder,
        IClock clock) =>
        ruleBuilder
            .Must(date => date is null || date <= clock.Today)
            .WithMessage("Date cannot be in the future.");

    public static IRuleBuilderOptions<T, string?> OptionalNotes<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        int maxLength = 1000) =>
        ruleBuilder.MaximumLength(maxLength);
}
