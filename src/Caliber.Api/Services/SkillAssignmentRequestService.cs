using Caliber.Api.Abstractions;
using Caliber.Api.Common;
using Caliber.Api.Data;
using Caliber.Api.Domain;
using Caliber.Api.Dtos.Settings;
using Microsoft.EntityFrameworkCore;

namespace Caliber.Api.Services;

public sealed class SkillAssignmentRequestService(
    CaliberDbContext db,
    IClock clock,
    ICurrentUser currentUser)
{
    public async Task<SkillAssignmentRequestDto> CreateAsync(
        int employeeId,
        CreateSkillAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureEmployeeAccessAsync(employeeId, cancellationToken);

        var skill = await db.Skills
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SkillId && s.IsActive, cancellationToken)
            ?? throw new NotFoundException("Skill", request.SkillId);

        var hasPending = await db.SkillAssignmentRequests.AnyAsync(
            x => x.EmployeeId == employeeId
                 && x.SkillId == request.SkillId
                 && x.Status == SkillRequestStatus.Pending,
            cancellationToken);

        if (hasPending)
        {
            throw new ConflictException("A pending skill request already exists for this employee.");
        }

        var entity = new SkillAssignmentRequest
        {
            EmployeeId = employeeId,
            SkillId = request.SkillId,
            RequestedProficiency = request.ProficiencyLevel,
            RequestedByEmployeeId = currentUser.EmployeeId,
            RequestedAt = clock.Now,
            Status = SkillRequestStatus.Pending,
            Notes = request.Notes,
        };

        db.SkillAssignmentRequests.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return await MapAsync(entity.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<SkillAssignmentRequestDto>> ListPendingAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureManagerOrAdmin();

        var query = db.SkillAssignmentRequests
            .AsNoTracking()
            .Where(x => x.Status == SkillRequestStatus.Pending);

        query = currentUser.AccessLevel switch
        {
            AccessLevel.Admin => query,
            AccessLevel.Manager => query.Where(x => x.Employee.LocationId == currentUser.LocationId),
            _ => query.Where(_ => false),
        };

        var ids = await query
            .OrderBy(x => x.RequestedAt)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var results = new List<SkillAssignmentRequestDto>();
        foreach (var id in ids)
        {
            results.Add(await MapAsync(id, cancellationToken));
        }

        return results;
    }

    public async Task<SkillAssignmentRequestDto> ApproveAsync(
        int requestId,
        ReviewSkillAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await LoadForReviewAsync(requestId, cancellationToken);

        entity.Status = SkillRequestStatus.Approved;
        entity.ReviewedByEmployeeId = currentUser.EmployeeId;
        entity.ReviewedAt = clock.Now;
        entity.ReviewNotes = request.ReviewNotes;

        var existing = await db.EmployeeSkills
            .SingleOrDefaultAsync(
                x => x.EmployeeId == entity.EmployeeId && x.SkillId == entity.SkillId,
                cancellationToken);

        if (existing is null)
        {
            db.EmployeeSkills.Add(new EmployeeSkill
            {
                EmployeeId = entity.EmployeeId,
                SkillId = entity.SkillId,
                ProficiencyLevel = entity.RequestedProficiency,
                SourceType = SkillSourceType.ManagerAssessed,
                AssessedOn = DateOnly.FromDateTime(clock.Now.DateTime),
                AssessedBy = currentUser.DisplayName,
                Notes = entity.Notes,
                Status = EmployeeSkillStatus.Active,
            });
        }
        else if (existing.ProficiencyLevel <= entity.RequestedProficiency)
        {
            existing.ProficiencyLevel = entity.RequestedProficiency;
            existing.SourceType = SkillSourceType.ManagerAssessed;
            existing.AssessedOn = DateOnly.FromDateTime(clock.Now.DateTime);
            existing.AssessedBy = currentUser.DisplayName;
            existing.Status = EmployeeSkillStatus.Active;
            existing.Notes = entity.Notes ?? existing.Notes;
        }

        await db.SaveChangesAsync(cancellationToken);
        return await MapAsync(entity.Id, cancellationToken);
    }

    public async Task<SkillAssignmentRequestDto> RejectAsync(
        int requestId,
        ReviewSkillAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await LoadForReviewAsync(requestId, cancellationToken);

        entity.Status = SkillRequestStatus.Rejected;
        entity.ReviewedByEmployeeId = currentUser.EmployeeId;
        entity.ReviewedAt = clock.Now;
        entity.ReviewNotes = request.ReviewNotes;

        await db.SaveChangesAsync(cancellationToken);
        return await MapAsync(entity.Id, cancellationToken);
    }

    private async Task<SkillAssignmentRequest> LoadForReviewAsync(
        int requestId,
        CancellationToken cancellationToken)
    {
        EnsureManagerOrAdmin();

        var entity = await db.SkillAssignmentRequests
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == requestId, cancellationToken)
            ?? throw new NotFoundException("SkillAssignmentRequest", requestId);

        if (entity.Status != SkillRequestStatus.Pending)
        {
            throw new BadRequestException("This skill request has already been reviewed.");
        }

        currentUser.EnsureCanAccessEmployee(entity.EmployeeId, entity.Employee.LocationId);
        return entity;
    }

    private async Task EnsureEmployeeAccessAsync(int employeeId, CancellationToken cancellationToken)
    {
        var employee = await db.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId && e.IsActive)
            .Select(e => new { e.Id, e.LocationId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Employee", employeeId);

        currentUser.EnsureCanAccessEmployee(employee.Id, employee.LocationId);
    }

    private void EnsureManagerOrAdmin()
    {
        if (currentUser.AccessLevel is not AccessLevel.Manager and not AccessLevel.Admin)
        {
            throw new ForbiddenException("Only managers and administrators may review skill requests.");
        }
    }

    private async Task<SkillAssignmentRequestDto> MapAsync(int id, CancellationToken cancellationToken)
    {
        var row = await db.SkillAssignmentRequests
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new SkillAssignmentRequestDto
            {
                Id = x.Id,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee.FirstName + " " + x.Employee.LastName,
                SkillId = x.SkillId,
                SkillName = x.Skill.Name,
                RequestedProficiency = x.RequestedProficiency,
                RequestedByName = x.RequestedBy.FirstName + " " + x.RequestedBy.LastName,
                RequestedAt = x.RequestedAt,
                Status = x.Status,
                Notes = x.Notes,
                ReviewNotes = x.ReviewNotes,
            })
            .FirstAsync(cancellationToken);

        return row;
    }
}
