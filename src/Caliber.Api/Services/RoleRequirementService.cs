using Caliber.Api.Abstractions;
using Caliber.Api.Common;
using Caliber.Api.Data;
using Caliber.Api.Domain;
using Caliber.Api.Dtos.Catalogues;
using Caliber.Api.Dtos.Requests;
using Microsoft.EntityFrameworkCore;

namespace Caliber.Api.Services;

public sealed class RoleRequirementService(
    CaliberDbContext db,
    IClock clock,
    ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<JobRoleDto>> ListRolesAsync(CancellationToken cancellationToken = default) =>
        await db.JobRoles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new JobRoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Department = r.Department.Name,
                DepartmentId = r.DepartmentId,
                Requirements = r.Requirements
                    .OrderBy(req => req.Kind)
                    .ThenBy(req => req.Id)
                    .Select(req => new RoleRequirementDto
                    {
                        Id = req.Id,
                        Kind = req.Kind,
                        CertificationId = req.CertificationId,
                        TrainingProgramId = req.TrainingProgramId,
                        SkillId = req.SkillId,
                        Name = req.Kind == RequirementKind.Certification
                            ? req.Certification!.Name
                            : req.Kind == RequirementKind.Training
                                ? req.TrainingProgram!.Name
                                : req.Skill!.Name,
                        MinimumProficiency = req.MinimumProficiency,
                        IsMandatory = req.IsMandatory,
                        DueWithinDaysOfHire = req.DueWithinDaysOfHire,
                    })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

    public async Task<JobRoleDto> GetRoleAsync(int jobRoleId, CancellationToken cancellationToken = default)
    {
        var role = await db.JobRoles
            .AsNoTracking()
            .Where(r => r.Id == jobRoleId)
            .Select(r => new JobRoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Department = r.Department.Name,
                DepartmentId = r.DepartmentId,
                Requirements = r.Requirements
                    .OrderBy(req => req.Kind)
                    .ThenBy(req => req.Id)
                    .Select(req => new RoleRequirementDto
                    {
                        Id = req.Id,
                        Kind = req.Kind,
                        CertificationId = req.CertificationId,
                        TrainingProgramId = req.TrainingProgramId,
                        SkillId = req.SkillId,
                        Name = req.Kind == RequirementKind.Certification
                            ? req.Certification!.Name
                            : req.Kind == RequirementKind.Training
                                ? req.TrainingProgram!.Name
                                : req.Skill!.Name,
                        MinimumProficiency = req.MinimumProficiency,
                        IsMandatory = req.IsMandatory,
                        DueWithinDaysOfHire = req.DueWithinDaysOfHire,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return role ?? throw new NotFoundException("JobRole", jobRoleId);
    }

    public async Task<RoleRequirementDto> AddRequirementAsync(
        int jobRoleId,
        AddRoleRequirementRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanMutateTemplates();

        if (request.Kind == RequirementKind.Skill)
        {
            throw new BadRequestException(
                "Direct skill requirements on roles are not supported. Add certifications or training that grant skills on completion.");
        }

        _ = await db.JobRoles
                .AsNoTracking()
                .AnyAsync(r => r.Id == jobRoleId, cancellationToken)
            ? true
            : throw new NotFoundException("JobRole", jobRoleId);

        await EnsureRequirementTargetExistsAsync(request, cancellationToken);
        await EnsureRequirementIsUniqueAsync(jobRoleId, request, cancellationToken);

        var requirement = new RoleRequirement
        {
            JobRoleId = jobRoleId,
            Kind = request.Kind,
            CertificationId = request.CertificationId,
            TrainingProgramId = request.TrainingProgramId,
            SkillId = request.SkillId,
            MinimumProficiency = request.MinimumProficiency,
            IsMandatory = request.IsMandatory,
            DueWithinDaysOfHire = request.DueWithinDaysOfHire,
        };

        db.RoleRequirements.Add(requirement);
        await db.SaveChangesAsync(cancellationToken);

        return await MapRequirementByIdAsync(requirement.Id, cancellationToken);
    }

    public async Task<ApplyRoleResultDto> ApplyRoleRequirementsAsync(
        int jobRoleId,
        CancellationToken cancellationToken = default)
    {
        EnsureCanMutateTemplates();

        var role = await db.JobRoles
            .Include(r => r.Requirements)
            .Include(r => r.Employees.Where(e => e.IsActive))
            .SingleOrDefaultAsync(r => r.Id == jobRoleId, cancellationToken)
            ?? throw new NotFoundException("JobRole", jobRoleId);

        var certificationsCreated = 0;
        var trainingsCreated = 0;

        foreach (var employee in role.Employees)
        {
            foreach (var requirement in role.Requirements)
            {
                switch (requirement.Kind)
                {
                    case RequirementKind.Certification when requirement.CertificationId is int certificationId:
                        if (!await db.EmployeeCertifications.AnyAsync(
                                x => x.EmployeeId == employee.Id && x.CertificationId == certificationId,
                                cancellationToken))
                        {
                            db.EmployeeCertifications.Add(new EmployeeCertification
                            {
                                EmployeeId = employee.Id,
                                CertificationId = certificationId,
                                Status = AssignmentStatus.NotStarted,
                                Source = AssignmentSource.RoleTemplate,
                                AssignedOn = clock.Today,
                                DueOn = requirement.DueWithinDaysOfHire is int certDays
                                    ? employee.HireDate.AddDays(certDays)
                                    : null,
                            });

                            certificationsCreated++;
                        }

                        break;

                    case RequirementKind.Training when requirement.TrainingProgramId is int trainingProgramId:
                        if (!await db.EmployeeTrainings.AnyAsync(
                                x => x.EmployeeId == employee.Id && x.TrainingProgramId == trainingProgramId,
                                cancellationToken))
                        {
                            db.EmployeeTrainings.Add(new EmployeeTraining
                            {
                                EmployeeId = employee.Id,
                                TrainingProgramId = trainingProgramId,
                                Status = AssignmentStatus.NotStarted,
                                Source = AssignmentSource.RoleTemplate,
                                AssignedOn = clock.Today,
                                DueOn = requirement.DueWithinDaysOfHire is int trainingDays
                                    ? employee.HireDate.AddDays(trainingDays)
                                    : null,
                                PercentComplete = 0,
                            });

                            trainingsCreated++;
                        }

                        break;

                    case RequirementKind.Skill:
                        // Skill requirements are evaluated against EmployeeSkills at read time;
                        // apply-to-role does not create duplicate skill records.
                        break;
                }
            }
        }

        if (certificationsCreated > 0 || trainingsCreated > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return new ApplyRoleResultDto
        {
            CertificationsCreated = certificationsCreated,
            TrainingsCreated = trainingsCreated,
        };
    }

    private async Task EnsureRequirementTargetExistsAsync(
        AddRoleRequirementRequest request,
        CancellationToken cancellationToken)
    {
        switch (request.Kind)
        {
            case RequirementKind.Certification:
                if (!await db.Certifications.AnyAsync(c => c.Id == request.CertificationId && c.IsActive, cancellationToken))
                {
                    throw new NotFoundException("Certification", request.CertificationId!);
                }

                break;

            case RequirementKind.Training:
                if (!await db.TrainingPrograms.AnyAsync(t => t.Id == request.TrainingProgramId && t.IsActive, cancellationToken))
                {
                    throw new NotFoundException("TrainingProgram", request.TrainingProgramId!);
                }

                break;

            case RequirementKind.Skill:
                if (!await db.Skills.AnyAsync(s => s.Id == request.SkillId && s.IsActive, cancellationToken))
                {
                    throw new NotFoundException("Skill", request.SkillId!);
                }

                break;
        }
    }

    private async Task EnsureRequirementIsUniqueAsync(
        int jobRoleId,
        AddRoleRequirementRequest request,
        CancellationToken cancellationToken)
    {
        var duplicate = request.Kind switch
        {
            RequirementKind.Certification => await db.RoleRequirements.AnyAsync(
                r => r.JobRoleId == jobRoleId
                     && r.Kind == RequirementKind.Certification
                     && r.CertificationId == request.CertificationId,
                cancellationToken),
            RequirementKind.Training => await db.RoleRequirements.AnyAsync(
                r => r.JobRoleId == jobRoleId
                     && r.Kind == RequirementKind.Training
                     && r.TrainingProgramId == request.TrainingProgramId,
                cancellationToken),
            RequirementKind.Skill => await db.RoleRequirements.AnyAsync(
                r => r.JobRoleId == jobRoleId
                     && r.Kind == RequirementKind.Skill
                     && r.SkillId == request.SkillId,
                cancellationToken),
            _ => false,
        };

        if (duplicate)
        {
            throw new ConflictException("This requirement is already defined for the role.");
        }
    }

    private async Task<RoleRequirementDto> MapRequirementByIdAsync(
        int requirementId,
        CancellationToken cancellationToken)
    {
        var requirement = await db.RoleRequirements
            .AsNoTracking()
            .Include(r => r.Certification)
            .Include(r => r.TrainingProgram)
            .Include(r => r.Skill)
            .FirstOrDefaultAsync(r => r.Id == requirementId, cancellationToken)
            ?? throw new NotFoundException("RoleRequirement", requirementId);

        return MapRequirement(requirement);
    }

    public async Task<IReadOnlyList<DepartmentDto>> ListDepartmentsAsync(
        CancellationToken cancellationToken = default) =>
        await db.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto { Id = d.Id, Name = d.Name })
            .ToListAsync(cancellationToken);

    public async Task<JobRoleDto> CreateRoleAsync(
        CreateJobRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanMutateTemplates();

        var name = request.Name.Trim();
        _ = await db.Departments
                .AsNoTracking()
                .AnyAsync(d => d.Id == request.DepartmentId, cancellationToken)
            ? true
            : throw new NotFoundException("Department", request.DepartmentId);

        if (await db.JobRoles.AnyAsync(r => r.Name == name, cancellationToken))
        {
            throw new ConflictException($"A job role named '{name}' already exists.");
        }

        var role = new JobRole
        {
            Name = name,
            DepartmentId = request.DepartmentId,
        };

        db.JobRoles.Add(role);
        await db.SaveChangesAsync(cancellationToken);

        return await GetRoleAsync(role.Id, cancellationToken);
    }

    public async Task<JobRoleDto> UpdateRoleAsync(
        int jobRoleId,
        UpdateJobRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanMutateTemplates();

        var role = await db.JobRoles.FirstOrDefaultAsync(r => r.Id == jobRoleId, cancellationToken)
            ?? throw new NotFoundException("JobRole", jobRoleId);

        if (request.Name is not null)
        {
            var name = request.Name.Trim();
            if (await db.JobRoles.AnyAsync(r => r.Id != jobRoleId && r.Name == name, cancellationToken))
            {
                throw new ConflictException($"A job role named '{name}' already exists.");
            }

            role.Name = name;
        }

        if (request.DepartmentId is not null)
        {
            _ = await db.Departments
                    .AsNoTracking()
                    .AnyAsync(d => d.Id == request.DepartmentId, cancellationToken)
                ? true
                : throw new NotFoundException("Department", request.DepartmentId.Value);

            role.DepartmentId = request.DepartmentId.Value;
        }

        await db.SaveChangesAsync(cancellationToken);

        return await GetRoleAsync(jobRoleId, cancellationToken);
    }

    public async Task DeleteRoleAsync(int jobRoleId, CancellationToken cancellationToken = default)
    {
        EnsureCanMutateTemplates();

        var role = await db.JobRoles
            .Include(r => r.Requirements)
            .FirstOrDefaultAsync(r => r.Id == jobRoleId, cancellationToken)
            ?? throw new NotFoundException("JobRole", jobRoleId);

        if (await db.Employees.AnyAsync(e => e.JobRoleId == jobRoleId, cancellationToken))
        {
            throw new ConflictException(
                "This job role cannot be deleted while employees are assigned. Reassign them first.");
        }

        db.RoleRequirements.RemoveRange(role.Requirements);
        db.JobRoles.Remove(role);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static RoleRequirementDto MapRequirement(RoleRequirement requirement) =>
        new()
        {
            Id = requirement.Id,
            Kind = requirement.Kind,
            CertificationId = requirement.CertificationId,
            TrainingProgramId = requirement.TrainingProgramId,
            SkillId = requirement.SkillId,
            Name = requirement.Kind switch
            {
                RequirementKind.Certification => requirement.Certification?.Name ?? string.Empty,
                RequirementKind.Training => requirement.TrainingProgram?.Name ?? string.Empty,
                RequirementKind.Skill => requirement.Skill?.Name ?? string.Empty,
                _ => string.Empty,
            },
            MinimumProficiency = requirement.MinimumProficiency,
            IsMandatory = requirement.IsMandatory,
            DueWithinDaysOfHire = requirement.DueWithinDaysOfHire,
        };

    private void EnsureCanMutateTemplates()
    {
        if (currentUser.AccessLevel == AccessLevel.Technician)
        {
            throw new ForbiddenException("Only managers and administrators may modify role requirement templates.");
        }
    }
}
