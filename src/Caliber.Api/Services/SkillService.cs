using Caliber.Api.Abstractions;
using Caliber.Api.Common;
using Caliber.Api.Data;
using Caliber.Api.Domain;
using Caliber.Api.Dtos.Catalogues;
using Caliber.Api.Dtos.Employees;
using Caliber.Api.Dtos.Requests;
using Microsoft.EntityFrameworkCore;

namespace Caliber.Api.Services;

public sealed class SkillService(
    CaliberDbContext db,
    IClock clock,
    ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<SkillDto>> ListCatalogueAsync(CancellationToken cancellationToken = default) =>
        await db.Skills
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new SkillDto
            {
                Id = s.Id,
                Name = s.Name,
                Category = s.Category,
                Description = s.Description,
                IsActive = s.IsActive,
            })
            .ToListAsync(cancellationToken);

    public async Task<SkillDto> GetCatalogueItemAsync(int skillId, CancellationToken cancellationToken = default)
    {
        var item = await db.Skills
            .AsNoTracking()
            .Where(s => s.Id == skillId)
            .Select(s => new SkillDto
            {
                Id = s.Id,
                Name = s.Name,
                Category = s.Category,
                Description = s.Description,
                IsActive = s.IsActive,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return item ?? throw new NotFoundException("Skill", skillId);
    }

    public async Task<SkillDto> CreateAsync(CreateSkillRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCanMutateCatalogue();

        if (await db.Skills.AnyAsync(s => s.Name == request.Name, cancellationToken))
        {
            throw new ConflictException($"A skill named '{request.Name}' already exists.");
        }

        var skill = new Skill
        {
            Name = request.Name,
            Category = request.Category,
            Description = request.Description,
            IsActive = true,
        };

        db.Skills.Add(skill);
        await db.SaveChangesAsync(cancellationToken);

        return await GetCatalogueItemAsync(skill.Id, cancellationToken);
    }

    public async Task<SkillDto> UpdateAsync(
        int skillId,
        UpdateSkillRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanMutateCatalogue();

        var skill = await db.Skills
            .FirstOrDefaultAsync(s => s.Id == skillId && s.IsActive, cancellationToken)
            ?? throw new NotFoundException("Skill", skillId);

        if (request.Name is not null && request.Name != skill.Name
            && await db.Skills.AnyAsync(s => s.Name == request.Name && s.Id != skillId, cancellationToken))
        {
            throw new ConflictException($"A skill named '{request.Name}' already exists.");
        }

        if (request.Name is not null)
        {
            skill.Name = request.Name;
        }

        if (request.Category is not null)
        {
            skill.Category = request.Category.Value;
        }

        if (request.Description is not null)
        {
            skill.Description = request.Description;
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetCatalogueItemAsync(skill.Id, cancellationToken);
    }

    public async Task DeactivateAsync(int skillId, CancellationToken cancellationToken = default)
    {
        EnsureCanMutateCatalogue();

        var skill = await db.Skills
            .FirstOrDefaultAsync(s => s.Id == skillId && s.IsActive, cancellationToken)
            ?? throw new NotFoundException("Skill", skillId);

        skill.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<EmployeeSkillDto> AssignOrAssessAsync(
        int employeeId,
        AssignSkillRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureEmployeeAccessAsync(employeeId, cancellationToken);

        if (currentUser.AccessLevel == AccessLevel.Technician)
        {
            throw new BadRequestException(
                "Technicians must submit a skill request for manager approval. Use POST /api/employees/{id}/skill-requests.");
        }

        var skillExists = await db.Skills
            .AsNoTracking()
            .AnyAsync(s => s.Id == request.SkillId && s.IsActive, cancellationToken);

        if (!skillExists)
        {
            throw new NotFoundException("Skill", request.SkillId);
        }

        var assessedOn = request.AssessedOn ?? clock.Today;

        var existing = await db.EmployeeSkills
            .SingleOrDefaultAsync(
                x => x.EmployeeId == employeeId && x.SkillId == request.SkillId,
                cancellationToken);

        if (existing is null)
        {
            existing = new EmployeeSkill
            {
                EmployeeId = employeeId,
                SkillId = request.SkillId,
                ProficiencyLevel = request.ProficiencyLevel,
                SourceType = SkillSourceType.ManagerAssessed,
                AssessedOn = assessedOn,
                AssessedBy = currentUser.DisplayName,
                Notes = request.Notes,
                Status = EmployeeSkillStatus.Active,
            };

            db.EmployeeSkills.Add(existing);
        }
        else if (request.ProficiencyLevel >= existing.ProficiencyLevel)
        {
            existing.ProficiencyLevel = request.ProficiencyLevel;
            existing.SourceType = SkillSourceType.ManagerAssessed;
            existing.AssessedOn = assessedOn;
            existing.AssessedBy = currentUser.DisplayName;
            existing.Notes = request.Notes ?? existing.Notes;
            existing.Status = EmployeeSkillStatus.Active;
        }
        else
        {
            throw new ConflictException(
                "The employee already holds this skill at higher proficiency from another source.");
        }

        await db.SaveChangesAsync(cancellationToken);

        return await MapEmployeeSkillAsync(existing.Id, cancellationToken);
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

    private void EnsureCanMutateCatalogue()
    {
        if (currentUser.AccessLevel == AccessLevel.Technician)
        {
            throw new ForbiddenException("Only managers and administrators may modify the skill catalogue.");
        }
    }

    private async Task<EmployeeSkillDto> MapEmployeeSkillAsync(int employeeSkillId, CancellationToken cancellationToken)
    {
        var row = await db.EmployeeSkills
            .AsNoTracking()
            .Where(x => x.Id == employeeSkillId)
            .Select(x => new
            {
                Assignment = x,
                Skill = x.Skill,
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("EmployeeSkill", employeeSkillId);

        return new EmployeeSkillDto
        {
            Id = row.Assignment.Id,
            SkillId = row.Skill.Id,
            SkillName = row.Skill.Name,
            Category = row.Skill.Category,
            ProficiencyLevel = row.Assignment.ProficiencyLevel,
            SourceType = row.Assignment.SourceType,
            SourceCertificationId = row.Assignment.SourceCertificationId,
            SourceTrainingProgramId = row.Assignment.SourceTrainingProgramId,
            AssessedOn = row.Assignment.AssessedOn,
            AssessedBy = row.Assignment.AssessedBy,
            Notes = row.Assignment.Notes,
            ExpiresOn = row.Assignment.ExpiresOn,
            Status = row.Assignment.Status,
            RowVersion = row.Assignment.RowVersion ?? [],
        };
    }
}
