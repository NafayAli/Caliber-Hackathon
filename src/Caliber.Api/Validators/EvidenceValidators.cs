using Caliber.Api.Domain;
using Caliber.Api.Dtos.Evidence;
using FluentValidation;

namespace Caliber.Api.Validators;

internal sealed class EvidenceUploadRequestValidator : AbstractValidator<EvidenceUploadRequest>
{
    public EvidenceUploadRequestValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.EvidenceType).IsInEnum();

        RuleFor(x => x)
            .Must(x => x.EvidenceType == EvidenceType.General
                       ? CountLinks(x) == 0
                       : CountLinks(x) == 1)
            .WithMessage("General evidence must not be linked to an assignment; all other types require exactly one assignment link.");

        RuleFor(x => x.EmployeeCertificationId)
            .GreaterThan(0)
            .When(x => x.EmployeeCertificationId is not null);

        RuleFor(x => x.EmployeeTrainingId)
            .GreaterThan(0)
            .When(x => x.EmployeeTrainingId is not null);

        RuleFor(x => x.EmployeeSkillId)
            .GreaterThan(0)
            .When(x => x.EmployeeSkillId is not null);
    }

    private static int CountLinks(EvidenceUploadRequest request)
    {
        var count = 0;
        if (request.EmployeeCertificationId is > 0)
        {
            count++;
        }

        if (request.EmployeeTrainingId is > 0)
        {
            count++;
        }

        if (request.EmployeeSkillId is > 0)
        {
            count++;
        }

        return count;
    }
}

internal sealed class VerifyEvidenceRequestValidator : AbstractValidator<VerifyEvidenceRequest>
{
    public VerifyEvidenceRequestValidator()
    {
        RuleFor(x => x.Notes).OptionalNotes();
    }
}
