using Caliber.Api.Abstractions;
using Caliber.Api.Common;
using Caliber.Api.Data;
using Caliber.Api.Domain;
using Caliber.Api.Dtos.Notifications;
using Caliber.Api.Dtos.Requests;
using Microsoft.EntityFrameworkCore;

namespace Caliber.Api.Services;

public sealed class RenewalService(
    CaliberDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    CertificationService certifications,
    TrainingService training,
    NotificationService notifications,
    ReadinessService readiness)
{
    public async Task<RenewalRequestDto> RequestRenewalAsync(
        CreateRenewalRequestBody request,
        CancellationToken cancellationToken = default)
    {
        var employeeId = currentUser.EmployeeId;
        await EnsureRenewableAsync(request.Kind, request.AssignmentId, employeeId, cancellationToken);

        var existing = await db.RenewalRequests
            .AsNoTracking()
            .AnyAsync(
                r => r.EmployeeId == employeeId
                     && r.Kind == request.Kind
                     && r.AssignmentId == request.AssignmentId
                     && r.Status == RenewalRequestStatus.Pending,
                cancellationToken);

        if (existing)
        {
            throw new ConflictException("A renewal request is already pending for this requirement.");
        }

        var (name, _) = await GetRequirementInfoAsync(request.Kind, request.AssignmentId, cancellationToken);

        var renewal = new RenewalRequest
        {
            EmployeeId = employeeId,
            Kind = request.Kind,
            AssignmentId = request.AssignmentId,
            Status = RenewalRequestStatus.Pending,
            EmployeeNote = request.Note?.Trim(),
            RequestedAt = clock.Now,
        };

        db.RenewalRequests.Add(renewal);
        await db.SaveChangesAsync(cancellationToken);

        var employee = await db.Employees.AsNoTracking().FirstAsync(e => e.Id == employeeId, cancellationToken);

        await notifications.NotifyManagersOfRenewalRequestAsync(
            renewal,
            employee.FullName,
            name,
            cancellationToken);

        return MapRenewal(renewal, name);
    }

    public async Task<RenewalRequestDto> ApproveAsync(
        int renewalRequestId,
        ReviewRenewalRequestBody request,
        CancellationToken cancellationToken = default)
    {
        EnsureManagerOrAdmin();

        var renewal = await db.RenewalRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == renewalRequestId, cancellationToken)
            ?? throw new NotFoundException("RenewalRequest", renewalRequestId);

        if (renewal.Status != RenewalRequestStatus.Pending)
        {
            throw new BadRequestException("This renewal request has already been reviewed.");
        }

        currentUser.EnsureCanAccessEmployee(renewal.EmployeeId, renewal.Employee.LocationId);

        await PerformRenewalAsync(renewal.Kind, renewal.AssignmentId, clock.Today, cancellationToken);

        renewal.Status = RenewalRequestStatus.Approved;
        renewal.ReviewerNote = request.Note?.Trim();
        renewal.ReviewedByEmployeeId = currentUser.EmployeeId;
        renewal.ReviewedAt = clock.Now;

        await db.SaveChangesAsync(cancellationToken);

        var (name, _) = await GetRequirementInfoAsync(renewal.Kind, renewal.AssignmentId, cancellationToken);

        await notifications.NotifyEmployeeAsync(
            renewal.EmployeeId,
            NotificationKind.RenewalDecision,
            "Renewal approved",
            $"Your renewal request for {name} was approved.",
            currentUser.EmployeeId,
            renewal.Id,
            cancellationToken);

        return MapRenewal(renewal, name);
    }

    public async Task<RenewalRequestDto> DeclineAsync(
        int renewalRequestId,
        ReviewRenewalRequestBody request,
        CancellationToken cancellationToken = default)
    {
        EnsureManagerOrAdmin();

        var renewal = await db.RenewalRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == renewalRequestId, cancellationToken)
            ?? throw new NotFoundException("RenewalRequest", renewalRequestId);

        if (renewal.Status != RenewalRequestStatus.Pending)
        {
            throw new BadRequestException("This renewal request has already been reviewed.");
        }

        currentUser.EnsureCanAccessEmployee(renewal.EmployeeId, renewal.Employee.LocationId);

        renewal.Status = RenewalRequestStatus.Declined;
        renewal.ReviewerNote = request.Note?.Trim();
        renewal.ReviewedByEmployeeId = currentUser.EmployeeId;
        renewal.ReviewedAt = clock.Now;

        await db.SaveChangesAsync(cancellationToken);

        var (name, _) = await GetRequirementInfoAsync(renewal.Kind, renewal.AssignmentId, cancellationToken);

        await notifications.NotifyEmployeeAsync(
            renewal.EmployeeId,
            NotificationKind.RenewalDecision,
            "Renewal declined",
            $"Your renewal request for {name} was declined.{(string.IsNullOrWhiteSpace(renewal.ReviewerNote) ? "" : $" Note: {renewal.ReviewerNote}")}",
            currentUser.EmployeeId,
            renewal.Id,
            cancellationToken);

        return MapRenewal(renewal, name);
    }

    public async Task DirectRenewAsync(
        DirectRenewRequestBody request,
        CancellationToken cancellationToken = default)
    {
        EnsureManagerOrAdmin();

        var employeeId = await GetAssignmentEmployeeIdAsync(request.Kind, request.AssignmentId, cancellationToken);
        var employee = await db.Employees.AsNoTracking().FirstAsync(e => e.Id == employeeId, cancellationToken);
        currentUser.EnsureCanAccessEmployee(employeeId, employee.LocationId);

        await EnsureRenewableAsync(request.Kind, request.AssignmentId, employeeId, cancellationToken);
        await PerformRenewalAsync(request.Kind, request.AssignmentId, request.RenewedOn ?? clock.Today, cancellationToken);

        var (name, _) = await GetRequirementInfoAsync(request.Kind, request.AssignmentId, cancellationToken);

        await notifications.NotifyEmployeeAsync(
            employeeId,
            NotificationKind.System,
            "Requirement renewed",
            $"{currentUser.DisplayName} renewed {name} for you.",
            currentUser.EmployeeId,
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<RenewalRequestDto>> ListPendingAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureManagerOrAdmin();

        var query = db.RenewalRequests
            .AsNoTracking()
            .Include(r => r.Employee)
            .Where(r => r.Status == RenewalRequestStatus.Pending);

        if (currentUser.AccessLevel == AccessLevel.Manager)
        {
            query = query.Where(r => r.Employee.LocationId == currentUser.LocationId);
        }

        var rows = await query.OrderBy(r => r.RequestedAt).ToListAsync(cancellationToken);
        var results = new List<RenewalRequestDto>();

        foreach (var row in rows)
        {
            var (name, _) = await GetRequirementInfoAsync(row.Kind, row.AssignmentId, cancellationToken);
            results.Add(MapRenewal(row, name));
        }

        return results;
    }

    private async Task PerformRenewalAsync(
        RequirementKind kind,
        int assignmentId,
        DateOnly renewedOn,
        CancellationToken cancellationToken)
    {
        if (kind == RequirementKind.Certification)
        {
            var assignment = await db.EmployeeCertifications
                .AsNoTracking()
                .FirstAsync(x => x.Id == assignmentId, cancellationToken);

            await certifications.RecordAwardAsync(
                assignmentId,
                new RecordAwardRequest { AwardedOn = renewedOn, RowVersion = assignment.RowVersion ?? [] },
                cancellationToken);
        }
        else if (kind == RequirementKind.Training)
        {
            var assignment = await db.EmployeeTrainings
                .AsNoTracking()
                .FirstAsync(x => x.Id == assignmentId, cancellationToken);

            await training.CompleteAsync(
                assignmentId,
                new CompleteTrainingRequest { CompletedOn = renewedOn, RowVersion = assignment.RowVersion ?? [] },
                cancellationToken);
        }
        else
        {
            throw new BadRequestException("Only certifications and training can be renewed.");
        }
    }

    private async Task EnsureRenewableAsync(
        RequirementKind kind,
        int assignmentId,
        int employeeId,
        CancellationToken cancellationToken)
    {
        var requirements = await readiness.GetRequirementsForEmployeeAsync(employeeId, cancellationToken);
        var match = requirements.FirstOrDefault(r => r.Kind == kind && r.SourceId == assignmentId)
            ?? throw new NotFoundException("Assignment", assignmentId);

        if (match.Status is not ReadinessStatus.ExpiringSoon and not ReadinessStatus.Expired)
        {
            throw new BadRequestException("Renewal is only available for expiring or expired requirements.");
        }
    }

    private async Task<int> GetAssignmentEmployeeIdAsync(
        RequirementKind kind,
        int assignmentId,
        CancellationToken cancellationToken) =>
        kind switch
        {
            RequirementKind.Certification => (await db.EmployeeCertifications.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == assignmentId, cancellationToken)
                ?? throw new NotFoundException("EmployeeCertification", assignmentId)).EmployeeId,
            RequirementKind.Training => (await db.EmployeeTrainings.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == assignmentId, cancellationToken)
                ?? throw new NotFoundException("EmployeeTraining", assignmentId)).EmployeeId,
            _ => throw new BadRequestException("Invalid requirement kind."),
        };

    private async Task<(string Name, int EmployeeId)> GetRequirementInfoAsync(
        RequirementKind kind,
        int assignmentId,
        CancellationToken cancellationToken)
    {
        if (kind == RequirementKind.Certification)
        {
            var row = await db.EmployeeCertifications
                .AsNoTracking()
                .Include(x => x.Certification)
                .FirstOrDefaultAsync(x => x.Id == assignmentId, cancellationToken)
                ?? throw new NotFoundException("EmployeeCertification", assignmentId);

            return (row.Certification.Name, row.EmployeeId);
        }

        var training = await db.EmployeeTrainings
            .AsNoTracking()
            .Include(x => x.TrainingProgram)
            .FirstOrDefaultAsync(x => x.Id == assignmentId, cancellationToken)
            ?? throw new NotFoundException("EmployeeTraining", assignmentId);

        return (training.TrainingProgram.Name, training.EmployeeId);
    }

    private static RenewalRequestDto MapRenewal(RenewalRequest renewal, string requirementName) =>
        new()
        {
            Id = renewal.Id,
            EmployeeId = renewal.EmployeeId,
            EmployeeName = renewal.Employee?.FullName ?? string.Empty,
            Kind = renewal.Kind,
            AssignmentId = renewal.AssignmentId,
            RequirementName = requirementName,
            Status = renewal.Status,
            EmployeeNote = renewal.EmployeeNote,
            ReviewerNote = renewal.ReviewerNote,
            RequestedAt = renewal.RequestedAt,
            ReviewedAt = renewal.ReviewedAt,
        };

    private void EnsureManagerOrAdmin()
    {
        if (currentUser.AccessLevel is not AccessLevel.Manager and not AccessLevel.Admin)
        {
            throw new ForbiddenException("Only managers and administrators may review renewals.");
        }
    }
}
