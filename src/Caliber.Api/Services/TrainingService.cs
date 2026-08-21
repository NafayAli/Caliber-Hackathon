using Caliber.Api.Abstractions;
using Caliber.Api.Common;
using Caliber.Api.Data;
using Caliber.Api.Domain;
using Caliber.Api.Dtos.Catalogues;
using Caliber.Api.Dtos.Employees;
using Caliber.Api.Dtos.Requests;
using Microsoft.EntityFrameworkCore;

namespace Caliber.Api.Services;

public sealed class TrainingService(
    CaliberDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    NotificationService notifications)
{
    public async Task<IReadOnlyList<TrainingProgramDto>> ListCatalogueAsync(CancellationToken cancellationToken = default) =>
        await db.TrainingPrograms
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new TrainingProgramDto
            {
                Id = t.Id,
                Name = t.Name,
                Code = t.Code,
                Category = t.Category,
                Provider = t.Provider,
                Description = t.Description,
                DeliveryMode = t.DeliveryMode,
                EstimatedDurationHours = t.EstimatedDurationHours,
                RequiresAcknowledgement = t.RequiresAcknowledgement,
                RecurrenceMonths = t.RecurrenceMonths,
                ExpiryWarningDays = t.ExpiryWarningDays,
                RequiresEvidence = t.RequiresEvidence,
                IsActive = t.IsActive,
                GrantedSkills = t.GrantedSkills
                    .Select(g => new GrantedSkillDto
                    {
                        SkillId = g.SkillId,
                        SkillName = g.Skill.Name,
                        GrantedProficiency = g.GrantedProficiency,
                    })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

    public async Task<TrainingProgramDto> GetCatalogueItemAsync(int trainingProgramId, CancellationToken cancellationToken = default)
    {
        var item = await db.TrainingPrograms
            .AsNoTracking()
            .Where(t => t.Id == trainingProgramId)
            .Select(t => new TrainingProgramDto
            {
                Id = t.Id,
                Name = t.Name,
                Code = t.Code,
                Category = t.Category,
                Provider = t.Provider,
                Description = t.Description,
                DeliveryMode = t.DeliveryMode,
                EstimatedDurationHours = t.EstimatedDurationHours,
                RequiresAcknowledgement = t.RequiresAcknowledgement,
                RecurrenceMonths = t.RecurrenceMonths,
                ExpiryWarningDays = t.ExpiryWarningDays,
                RequiresEvidence = t.RequiresEvidence,
                IsActive = t.IsActive,
                GrantedSkills = t.GrantedSkills
                    .Select(g => new GrantedSkillDto
                    {
                        SkillId = g.SkillId,
                        SkillName = g.Skill.Name,
                        GrantedProficiency = g.GrantedProficiency,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return item ?? throw new NotFoundException("TrainingProgram", trainingProgramId);
    }

    public async Task<TrainingProgramDto> CreateAsync(
        CreateTrainingProgramRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanMutateCatalogue();

        if (await db.TrainingPrograms.AnyAsync(t => t.Code == request.Code, cancellationToken))
        {
            throw new ConflictException($"A training program with code '{request.Code}' already exists.");
        }

        var program = new TrainingProgram
        {
            Name = request.Name,
            Code = request.Code,
            Category = request.Category,
            Provider = request.Provider,
            Description = request.Description,
            DeliveryMode = request.DeliveryMode,
            EstimatedDurationHours = request.EstimatedDurationHours,
            RequiresAcknowledgement = request.RequiresAcknowledgement,
            RecurrenceMonths = request.RecurrenceMonths,
            ExpiryWarningDays = request.ExpiryWarningDays,
            RequiresEvidence = request.RequiresEvidence,
            IsActive = true,
        };

        db.TrainingPrograms.Add(program);
        await db.SaveChangesAsync(cancellationToken);

        if (request.GrantedSkills.Count > 0)
        {
            await SkillGrantingHelper.ApplyTrainingGrantedSkillsAsync(
                db,
                program.Id,
                request.GrantedSkills,
                replaceAll: true,
                cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        return await GetCatalogueItemAsync(program.Id, cancellationToken);
    }

    public async Task<TrainingProgramDto> UpdateAsync(
        int trainingProgramId,
        UpdateTrainingProgramRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanMutateCatalogue();

        var program = await db.TrainingPrograms
            .FirstOrDefaultAsync(t => t.Id == trainingProgramId && t.IsActive, cancellationToken)
            ?? throw new NotFoundException("TrainingProgram", trainingProgramId);

        if (request.Code is not null && request.Code != program.Code
            && await db.TrainingPrograms.AnyAsync(t => t.Code == request.Code && t.Id != trainingProgramId, cancellationToken))
        {
            throw new ConflictException($"A training program with code '{request.Code}' already exists.");
        }

        if (request.Name is not null)
        {
            program.Name = request.Name;
        }

        if (request.Code is not null)
        {
            program.Code = request.Code;
        }

        if (request.Category is not null)
        {
            program.Category = request.Category.Value;
        }

        if (request.Provider is not null)
        {
            program.Provider = request.Provider;
        }

        if (request.Description is not null)
        {
            program.Description = request.Description;
        }

        if (request.DeliveryMode is not null)
        {
            program.DeliveryMode = request.DeliveryMode.Value;
        }

        if (request.EstimatedDurationHours is not null)
        {
            program.EstimatedDurationHours = request.EstimatedDurationHours.Value;
        }

        if (request.RequiresAcknowledgement is not null)
        {
            program.RequiresAcknowledgement = request.RequiresAcknowledgement.Value;
        }

        if (request.RecurrenceMonths is not null)
        {
            program.RecurrenceMonths = request.RecurrenceMonths;
        }

        if (request.ExpiryWarningDays is not null)
        {
            program.ExpiryWarningDays = request.ExpiryWarningDays.Value;
        }

        if (request.RequiresEvidence is not null)
        {
            program.RequiresEvidence = request.RequiresEvidence.Value;
        }

        await db.SaveChangesAsync(cancellationToken);

        return await GetCatalogueItemAsync(program.Id, cancellationToken);
    }

    public async Task DeactivateAsync(int trainingProgramId, CancellationToken cancellationToken = default)
    {
        EnsureCanMutateCatalogue();

        var program = await db.TrainingPrograms
            .FirstOrDefaultAsync(t => t.Id == trainingProgramId && t.IsActive, cancellationToken)
            ?? throw new NotFoundException("TrainingProgram", trainingProgramId);

        program.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<TrainingProgramDto> SetGrantedSkillsAsync(
        int trainingProgramId,
        SetGrantedSkillsRequest request,
        bool replaceAll,
        CancellationToken cancellationToken = default)
    {
        EnsureCanMutateCatalogue();

        var exists = await db.TrainingPrograms
            .AnyAsync(t => t.Id == trainingProgramId && t.IsActive, cancellationToken);

        if (!exists)
        {
            throw new NotFoundException("TrainingProgram", trainingProgramId);
        }

        await SkillGrantingHelper.ApplyTrainingGrantedSkillsAsync(
            db,
            trainingProgramId,
            request.Grants,
            replaceAll,
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return await GetCatalogueItemAsync(trainingProgramId, cancellationToken);
    }

    public async Task<EmployeeTrainingDto> AssignAsync(
        int employeeId,
        AssignTrainingRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureEmployeeAccessAsync(employeeId, cancellationToken);

        var programExists = await db.TrainingPrograms
            .AsNoTracking()
            .AnyAsync(t => t.Id == request.TrainingProgramId && t.IsActive, cancellationToken);

        if (!programExists)
        {
            throw new NotFoundException("TrainingProgram", request.TrainingProgramId);
        }

        if (await db.EmployeeTrainings.AnyAsync(
                x => x.EmployeeId == employeeId && x.TrainingProgramId == request.TrainingProgramId,
                cancellationToken))
        {
            throw new ConflictException("This training program is already assigned to the employee.");
        }

        var assignment = new EmployeeTraining
        {
            EmployeeId = employeeId,
            TrainingProgramId = request.TrainingProgramId,
            Status = AssignmentStatus.NotStarted,
            Source = AssignmentSource.Direct,
            AssignedOn = clock.Today,
            DueOn = request.DueOn,
            Notes = request.Notes,
            PercentComplete = 0,
        };

        db.EmployeeTrainings.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);

        return await MapAssignmentAsync(assignment.Id, cancellationToken);
    }

    public async Task<EmployeeTrainingDto> UpdateProgressAsync(
        int employeeTrainingId,
        UpdateTrainingProgressRequest request,
        CancellationToken cancellationToken = default)
    {
        var assignment = await LoadAssignmentAsync(employeeTrainingId, cancellationToken);

        if (assignment.Status is AssignmentStatus.Completed or AssignmentStatus.Waived)
        {
            throw new BadRequestException("Completed or waived training cannot be updated.");
        }

        db.Entry(assignment).Property(x => x.RowVersion).OriginalValue = request.RowVersion;

        if (request.Status is not null)
        {
            assignment.Status = request.Status.Value;
        }

        if (request.PercentComplete is not null)
        {
            assignment.PercentComplete = request.PercentComplete.Value;
            if (assignment.PercentComplete > 0 && assignment.Status == AssignmentStatus.NotStarted)
            {
                assignment.Status = AssignmentStatus.InProgress;
            }
        }

        if (request.StartedOn is not null)
        {
            assignment.StartedOn = request.StartedOn;
            if (assignment.Status == AssignmentStatus.NotStarted)
            {
                assignment.Status = AssignmentStatus.InProgress;
            }
        }

        if (request.Notes is not null)
        {
            assignment.Notes = request.Notes;
        }

        await db.SaveChangesAsync(cancellationToken);

        return await MapAssignmentAsync(assignment.Id, cancellationToken);
    }

    public async Task<EmployeeTrainingDto> CompleteAsync(
        int employeeTrainingId,
        CompleteTrainingRequest request,
        CancellationToken cancellationToken = default)
    {
        var assignment = await LoadAssignmentWithProgramAsync(employeeTrainingId, cancellationToken);

        if (assignment.Status == AssignmentStatus.Waived)
        {
            throw new BadRequestException("Waived training cannot be completed.");
        }

        db.Entry(assignment).Property(x => x.RowVersion).OriginalValue = request.RowVersion;

        var completedOn = request.CompletedOn ?? clock.Today;
        assignment.Status = AssignmentStatus.Completed;
        assignment.CompletedOn = completedOn;
        assignment.PercentComplete = 100;
        assignment.Score = request.Score;
        assignment.Notes = request.Notes ?? assignment.Notes;

        assignment.NextDueOn = assignment.TrainingProgram.RecurrenceMonths is int months
            ? completedOn.AddMonths(months)
            : null;

        await SkillGrantingHelper.GrantSkillsFromTrainingAsync(
            db,
            assignment.EmployeeId,
            assignment.TrainingProgramId,
            assignment.Id,
            assignment.NextDueOn,
            completedOn,
            currentUser.DisplayName,
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return await MapAssignmentAsync(assignment.Id, cancellationToken);
    }

    public async Task<EmployeeTrainingDto> AcknowledgeAsync(
        int employeeTrainingId,
        AcknowledgeTrainingRequest request,
        CancellationToken cancellationToken = default)
    {
        var assignment = await LoadAssignmentWithProgramAsync(employeeTrainingId, cancellationToken);

        if (!assignment.TrainingProgram.RequiresAcknowledgement)
        {
            throw new BadRequestException("This training program does not require acknowledgement.");
        }

        if (assignment.Status != AssignmentStatus.Completed)
        {
            throw new BadRequestException("Training must be completed before it can be acknowledged.");
        }

        db.Entry(assignment).Property(x => x.RowVersion).OriginalValue = request.RowVersion;

        assignment.AcknowledgedOn = clock.Now;
        assignment.AcknowledgedBy = currentUser.DisplayName;

        await db.SaveChangesAsync(cancellationToken);

        await notifications.NotifyManagersOfAcknowledgementAsync(
            assignment.EmployeeId,
            assignment.Employee.FullName,
            assignment.TrainingProgram.Name,
            assignment.Id,
            cancellationToken);

        return await MapAssignmentAsync(assignment.Id, cancellationToken);
    }

    private async Task<EmployeeTraining> LoadAssignmentAsync(int employeeTrainingId, CancellationToken cancellationToken)
    {
        var assignment = await db.EmployeeTrainings
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == employeeTrainingId, cancellationToken)
            ?? throw new NotFoundException("EmployeeTraining", employeeTrainingId);

        currentUser.EnsureCanAccessEmployee(assignment.EmployeeId, assignment.Employee.LocationId);
        return assignment;
    }

    private async Task<EmployeeTraining> LoadAssignmentWithProgramAsync(
        int employeeTrainingId,
        CancellationToken cancellationToken)
    {
        var assignment = await db.EmployeeTrainings
            .Include(x => x.Employee)
            .Include(x => x.TrainingProgram)
            .FirstOrDefaultAsync(x => x.Id == employeeTrainingId, cancellationToken)
            ?? throw new NotFoundException("EmployeeTraining", employeeTrainingId);

        currentUser.EnsureCanAccessEmployee(assignment.EmployeeId, assignment.Employee.LocationId);
        return assignment;
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
            throw new ForbiddenException("Only managers and administrators may modify the training catalogue.");
        }
    }

    private async Task<EmployeeTrainingDto> MapAssignmentAsync(int employeeTrainingId, CancellationToken cancellationToken)
    {
        var row = await db.EmployeeTrainings
            .AsNoTracking()
            .Where(x => x.Id == employeeTrainingId)
            .Select(x => new
            {
                Assignment = x,
                Program = x.TrainingProgram,
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("EmployeeTraining", employeeTrainingId);

        var isCompleted = row.Assignment.Status == AssignmentStatus.Completed
                          || row.Assignment.Status == AssignmentStatus.Waived
                          || row.Assignment.CompletedOn is not null;

        var readinessStatus = ReadinessService.ComputeStatus(
            row.Assignment.Status,
            isCompleted,
            row.Assignment.NextDueOn,
            row.Assignment.DueOn,
            row.Program.ExpiryWarningDays,
            clock.Today);

        return new EmployeeTrainingDto
        {
            Id = row.Assignment.Id,
            TrainingProgramId = row.Program.Id,
            TrainingProgramName = row.Program.Name,
            TrainingProgramCode = row.Program.Code,
            Status = row.Assignment.Status,
            Source = row.Assignment.Source,
            AssignedOn = row.Assignment.AssignedOn,
            DueOn = row.Assignment.DueOn,
            StartedOn = row.Assignment.StartedOn,
            CompletedOn = row.Assignment.CompletedOn,
            NextDueOn = row.Assignment.NextDueOn,
            AcknowledgedOn = row.Assignment.AcknowledgedOn,
            AcknowledgedBy = row.Assignment.AcknowledgedBy,
            PercentComplete = row.Assignment.PercentComplete,
            Score = row.Assignment.Score,
            Notes = row.Assignment.Notes,
            ReadinessStatus = readinessStatus,
            RowVersion = row.Assignment.RowVersion ?? [],
        };
    }
}
