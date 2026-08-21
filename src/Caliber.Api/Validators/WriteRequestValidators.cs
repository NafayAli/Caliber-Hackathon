using Caliber.Api.Abstractions;
using Caliber.Api.Domain;
using Caliber.Api.Dtos.Requests;
using FluentValidation;

namespace Caliber.Api.Validators;

internal sealed class AssignCertificationRequestValidator : AbstractValidator<AssignCertificationRequest>
{
    public AssignCertificationRequestValidator(IClock clock)
    {
        RuleFor(x => x.CertificationId).GreaterThan(0);
        RuleFor(x => x.DueOn).NotInFutureWhenSet(clock);
        RuleFor(x => x.Notes).OptionalNotes();
    }
}

internal sealed class RecordAwardRequestValidator : AbstractValidator<RecordAwardRequest>
{
    public RecordAwardRequestValidator(IClock clock)
    {
        RuleFor(x => x.AwardedOn).NotInFuture(clock);
        RuleFor(x => x.CertificateNumber).MaximumLength(80).When(x => x.CertificateNumber is not null);
        RuleFor(x => x.Notes).OptionalNotes();
        RuleFor(x => x.RowVersion).ValidRowVersion();
    }
}

internal sealed class WaiveAssignmentRequestValidator : AbstractValidator<WaiveAssignmentRequest>
{
    public WaiveAssignmentRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.RowVersion).ValidRowVersion();
    }
}

internal sealed class AssignTrainingRequestValidator : AbstractValidator<AssignTrainingRequest>
{
    public AssignTrainingRequestValidator(IClock clock)
    {
        RuleFor(x => x.TrainingProgramId).GreaterThan(0);
        RuleFor(x => x.DueOn).NotInFutureWhenSet(clock);
        RuleFor(x => x.Notes).OptionalNotes();
    }
}

internal sealed class AssignSkillRequestValidator : AbstractValidator<AssignSkillRequest>
{
    public AssignSkillRequestValidator(IClock clock)
    {
        RuleFor(x => x.SkillId).GreaterThan(0);
        RuleFor(x => x.ProficiencyLevel).IsInEnum();
        RuleFor(x => x.AssessedOn).NotInFutureWhenSet(clock);
        RuleFor(x => x.Notes).OptionalNotes();
    }
}

internal sealed class UpdateTrainingProgressRequestValidator : AbstractValidator<UpdateTrainingProgressRequest>
{
    public UpdateTrainingProgressRequestValidator(IClock clock)
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .Must(status => status is null
                            or AssignmentStatus.NotStarted
                            or AssignmentStatus.InProgress)
            .WithMessage("Status may only be set to NotStarted or InProgress.")
            .When(x => x.Status is not null);

        RuleFor(x => x.PercentComplete)
            .InclusiveBetween(0, 100)
            .When(x => x.PercentComplete is not null);

        RuleFor(x => x.StartedOn).NotInFutureWhenSet(clock);
        RuleFor(x => x.Notes).OptionalNotes();
        RuleFor(x => x.RowVersion).ValidRowVersion();

        RuleFor(x => x)
            .Must(x => x.Status is not null || x.PercentComplete is not null || x.StartedOn is not null || x.Notes is not null)
            .WithMessage("At least one field must be provided to update.");
    }
}

internal sealed class CompleteTrainingRequestValidator : AbstractValidator<CompleteTrainingRequest>
{
    public CompleteTrainingRequestValidator(IClock clock)
    {
        RuleFor(x => x.CompletedOn).NotInFutureWhenSet(clock);
        RuleFor(x => x.Score).InclusiveBetween(0, 100).When(x => x.Score is not null);
        RuleFor(x => x.Notes).OptionalNotes();
        RuleFor(x => x.RowVersion).ValidRowVersion();
    }
}

internal sealed class AcknowledgeTrainingRequestValidator : AbstractValidator<AcknowledgeTrainingRequest>
{
    public AcknowledgeTrainingRequestValidator()
    {
        RuleFor(x => x.RowVersion).ValidRowVersion();
    }
}

internal sealed class AddRoleRequirementRequestValidator : AbstractValidator<AddRoleRequirementRequest>
{
    public AddRoleRequirementRequestValidator()
    {
        RuleFor(x => x.Kind).IsInEnum();

        RuleFor(x => x.CertificationId)
            .GreaterThan(0)
            .When(x => x.Kind == RequirementKind.Certification);

        RuleFor(x => x.TrainingProgramId)
            .GreaterThan(0)
            .When(x => x.Kind == RequirementKind.Training);

        RuleFor(x => x.SkillId)
            .GreaterThan(0)
            .When(x => x.Kind == RequirementKind.Skill);

        RuleFor(x => x.MinimumProficiency)
            .IsInEnum()
            .When(x => x.Kind == RequirementKind.Skill);

        RuleFor(x => x.MinimumProficiency)
            .Null()
            .When(x => x.Kind != RequirementKind.Skill);

        RuleFor(x => x.DueWithinDaysOfHire)
            .GreaterThan(0)
            .When(x => x.DueWithinDaysOfHire is not null);

        RuleFor(x => x)
            .Must(x => x.Kind switch
            {
                RequirementKind.Certification => x.CertificationId is > 0
                                                 && x.TrainingProgramId is null
                                                 && x.SkillId is null,
                RequirementKind.Training => x.TrainingProgramId is > 0
                                              && x.CertificationId is null
                                              && x.SkillId is null,
                RequirementKind.Skill => x.SkillId is > 0
                                         && x.CertificationId is null
                                         && x.TrainingProgramId is null,
                _ => false,
            })
            .WithMessage("Exactly one catalogue target must match the requirement kind.");
    }
}

internal sealed class CreateJobRoleRequestValidator : AbstractValidator<CreateJobRoleRequest>
{
    public CreateJobRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DepartmentId).GreaterThan(0);
    }
}

internal sealed class UpdateJobRoleRequestValidator : AbstractValidator<UpdateJobRoleRequest>
{
    public UpdateJobRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).When(x => x.Name is not null);
        RuleFor(x => x.DepartmentId).GreaterThan(0).When(x => x.DepartmentId is not null);
        RuleFor(x => x)
            .Must(x => x.Name is not null || x.DepartmentId is not null)
            .WithMessage("At least one field must be provided.");
    }
}

internal sealed class CreateCertificationRequestValidator : AbstractValidator<CreateCertificationRequest>
{
    public CreateCertificationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.IssuingBody).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
        RuleFor(x => x.ValidityMonths).GreaterThan(0).When(x => x.ValidityMonths is not null);
        RuleFor(x => x.ExpiryWarningDays).GreaterThan(0);
        RuleForEach(x => x.GrantedSkills).SetValidator(new SkillGrantInputValidator());
    }
}

internal sealed class CreateTrainingProgramRequestValidator : AbstractValidator<CreateTrainingProgramRequest>
{
    public CreateTrainingProgramRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Provider).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
        RuleFor(x => x.DeliveryMode).IsInEnum();
        RuleFor(x => x.EstimatedDurationHours).GreaterThan(0);
        RuleFor(x => x.RecurrenceMonths).GreaterThan(0).When(x => x.RecurrenceMonths is not null);
        RuleFor(x => x.ExpiryWarningDays).GreaterThan(0);
        RuleForEach(x => x.GrantedSkills).SetValidator(new SkillGrantInputValidator());
    }
}

internal sealed class CreateSkillRequestValidator : AbstractValidator<CreateSkillRequest>
{
    public CreateSkillRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
    }
}

internal sealed class UpdateSkillRequestValidator : AbstractValidator<UpdateSkillRequest>
{
    public UpdateSkillRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120).When(x => x.Name is not null);
        RuleFor(x => x.Category).IsInEnum().When(x => x.Category is not null);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
    }
}

internal sealed class SkillGrantInputValidator : AbstractValidator<SkillGrantInput>
{
    public SkillGrantInputValidator()
    {
        RuleFor(x => x.SkillId).GreaterThan(0);
        RuleFor(x => x.GrantedProficiency).IsInEnum();
    }
}

internal sealed class SetGrantedSkillsRequestValidator : AbstractValidator<SetGrantedSkillsRequest>
{
    public SetGrantedSkillsRequestValidator()
    {
        RuleForEach(x => x.Grants).SetValidator(new SkillGrantInputValidator());
        RuleFor(x => x.Grants)
            .Must(grants => grants.Select(g => g.SkillId).Distinct().Count() == grants.Count)
            .WithMessage("Duplicate skill grants are not allowed.");
    }
}

internal sealed class UpdateCertificationRequestValidator : AbstractValidator<UpdateCertificationRequest>
{
    public UpdateCertificationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150).When(x => x.Name is not null);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30).When(x => x.Code is not null);
        RuleFor(x => x.Category).IsInEnum().When(x => x.Category is not null);
        RuleFor(x => x.IssuingBody).NotEmpty().MaximumLength(120).When(x => x.IssuingBody is not null);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
        RuleFor(x => x.ValidityMonths).GreaterThan(0).When(x => x.ValidityMonths is not null);
        RuleFor(x => x.ExpiryWarningDays).GreaterThan(0).When(x => x.ExpiryWarningDays is not null);
        RuleFor(x => x)
            .Must(x => x.Name is not null
                       || x.Code is not null
                       || x.Category is not null
                       || x.IssuingBody is not null
                       || x.Description is not null
                       || x.ValidityMonths is not null
                       || x.ExpiryWarningDays is not null
                       || x.RequiresEvidence is not null)
            .WithMessage("At least one field must be provided to update.");
    }
}

internal sealed class UpdateTrainingProgramRequestValidator : AbstractValidator<UpdateTrainingProgramRequest>
{
    public UpdateTrainingProgramRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150).When(x => x.Name is not null);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30).When(x => x.Code is not null);
        RuleFor(x => x.Category).IsInEnum().When(x => x.Category is not null);
        RuleFor(x => x.Provider).NotEmpty().MaximumLength(120).When(x => x.Provider is not null);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
        RuleFor(x => x.DeliveryMode).IsInEnum().When(x => x.DeliveryMode is not null);
        RuleFor(x => x.EstimatedDurationHours).GreaterThan(0).When(x => x.EstimatedDurationHours is not null);
        RuleFor(x => x.RecurrenceMonths).GreaterThan(0).When(x => x.RecurrenceMonths is not null);
        RuleFor(x => x.ExpiryWarningDays).GreaterThan(0).When(x => x.ExpiryWarningDays is not null);
        RuleFor(x => x)
            .Must(x => x.Name is not null
                       || x.Code is not null
                       || x.Category is not null
                       || x.Provider is not null
                       || x.Description is not null
                       || x.DeliveryMode is not null
                       || x.EstimatedDurationHours is not null
                       || x.RequiresAcknowledgement is not null
                       || x.RecurrenceMonths is not null
                       || x.ExpiryWarningDays is not null
                       || x.RequiresEvidence is not null)
            .WithMessage("At least one field must be provided to update.");
    }
}

internal sealed class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    public CreateEmployeeRequestValidator(IClock clock)
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(60);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(200);
        RuleFor(x => x.JobRoleId).GreaterThan(0);
        RuleFor(x => x.LocationId).GreaterThan(0);
        RuleFor(x => x.ExternalEmployeeNo).MaximumLength(40).When(x => x.ExternalEmployeeNo is not null);
        RuleFor(x => x.HireDate).NotInFutureWhenSet(clock);
        RuleFor(x => x.AccessLevel).IsInEnum();
    }
}

internal sealed class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequest>
{
    public UpdateEmployeeRequestValidator(IClock clock)
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(60).When(x => x.FirstName is not null);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(60).When(x => x.LastName is not null);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200).When(x => x.Email is not null);
        RuleFor(x => x.JobRoleId).GreaterThan(0).When(x => x.JobRoleId is not null);
        RuleFor(x => x.LocationId).GreaterThan(0).When(x => x.LocationId is not null);
        RuleFor(x => x.ExternalEmployeeNo).MaximumLength(40).When(x => x.ExternalEmployeeNo is not null);
        RuleFor(x => x.HireDate).NotInFutureWhenSet(clock);
        RuleFor(x => x.AccessLevel).IsInEnum().When(x => x.AccessLevel is not null);
        RuleFor(x => x)
            .Must(x => x.FirstName is not null
                       || x.LastName is not null
                       || x.Email is not null
                       || x.JobRoleId is not null
                       || x.LocationId is not null
                       || x.ExternalEmployeeNo is not null
                       || x.HireDate is not null
                       || x.AccessLevel is not null
                       || x.IsActive is not null)
            .WithMessage("At least one field must be provided to update.");
    }
}

internal sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(60).When(x => x.FirstName is not null);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(60).When(x => x.LastName is not null);
        RuleFor(x => x.Phone).MaximumLength(30).When(x => x.Phone is not null);
        RuleFor(x => x.Bio).MaximumLength(1000).When(x => x.Bio is not null);
        RuleFor(x => x)
            .Must(x => x.FirstName is not null
                       || x.LastName is not null
                       || x.Phone is not null
                       || x.Bio is not null)
            .WithMessage("At least one field must be provided to update.");
    }
}
