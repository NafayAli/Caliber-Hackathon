using Caliber.Api.Common;
using Caliber.Api.Data;
using Caliber.Api.Domain;
using Caliber.Api.Dtos.Requests;
using Microsoft.EntityFrameworkCore;

namespace Caliber.Api.Services;

internal static class SkillGrantingHelper
{
    public static async Task ApplyCertificationGrantedSkillsAsync(
        CaliberDbContext db,
        int certificationId,
        IReadOnlyList<SkillGrantInput> grants,
        bool replaceAll,
        CancellationToken cancellationToken)
    {
        await ValidateSkillGrantsAsync(db, grants, cancellationToken);

        var existing = await db.CertificationSkills
            .Where(x => x.CertificationId == certificationId)
            .ToListAsync(cancellationToken);

        if (replaceAll)
        {
            db.CertificationSkills.RemoveRange(existing);
            existing.Clear();
        }

        foreach (var grant in grants)
        {
            var match = existing.SingleOrDefault(x => x.SkillId == grant.SkillId);
            if (match is null)
            {
                db.CertificationSkills.Add(new CertificationSkill
                {
                    CertificationId = certificationId,
                    SkillId = grant.SkillId,
                    GrantedProficiency = grant.GrantedProficiency,
                });
            }
            else
            {
                match.GrantedProficiency = grant.GrantedProficiency;
            }
        }
    }

    public static async Task ApplyTrainingGrantedSkillsAsync(
        CaliberDbContext db,
        int trainingProgramId,
        IReadOnlyList<SkillGrantInput> grants,
        bool replaceAll,
        CancellationToken cancellationToken)
    {
        await ValidateSkillGrantsAsync(db, grants, cancellationToken);

        var existing = await db.TrainingProgramSkills
            .Where(x => x.TrainingProgramId == trainingProgramId)
            .ToListAsync(cancellationToken);

        if (replaceAll)
        {
            db.TrainingProgramSkills.RemoveRange(existing);
            existing.Clear();
        }

        foreach (var grant in grants)
        {
            var match = existing.SingleOrDefault(x => x.SkillId == grant.SkillId);
            if (match is null)
            {
                db.TrainingProgramSkills.Add(new TrainingProgramSkill
                {
                    TrainingProgramId = trainingProgramId,
                    SkillId = grant.SkillId,
                    GrantedProficiency = grant.GrantedProficiency,
                });
            }
            else
            {
                match.GrantedProficiency = grant.GrantedProficiency;
            }
        }
    }

    private static async Task ValidateSkillGrantsAsync(
        CaliberDbContext db,
        IReadOnlyList<SkillGrantInput> grants,
        CancellationToken cancellationToken)
    {
        if (grants.Count == 0)
        {
            return;
        }

        if (grants.Select(g => g.SkillId).Distinct().Count() != grants.Count)
        {
            throw new BadRequestException("Duplicate skill grants are not allowed.");
        }

        var skillIds = grants.Select(g => g.SkillId).ToArray();
        var activeSkillCount = await db.Skills
            .AsNoTracking()
            .CountAsync(s => skillIds.Contains(s.Id) && s.IsActive, cancellationToken);

        if (activeSkillCount != skillIds.Length)
        {
            throw new BadRequestException("One or more granted skills do not exist or are inactive.");
        }
    }

    public static async Task GrantSkillsFromCertificationAsync(
        CaliberDbContext db,
        int employeeId,
        int certificationId,
        int employeeCertificationId,
        DateOnly? expiresOn,
        DateOnly assessedOn,
        string assessedBy,
        CancellationToken cancellationToken)
    {
        var grants = await db.CertificationSkills
            .AsNoTracking()
            .Where(x => x.CertificationId == certificationId)
            .ToListAsync(cancellationToken);

        foreach (var grant in grants)
        {
            var existing = await db.EmployeeSkills
                .SingleOrDefaultAsync(
                    x => x.EmployeeId == employeeId && x.SkillId == grant.SkillId,
                    cancellationToken);

            if (existing is null)
            {
                db.EmployeeSkills.Add(new EmployeeSkill
                {
                    EmployeeId = employeeId,
                    SkillId = grant.SkillId,
                    ProficiencyLevel = grant.GrantedProficiency,
                    SourceType = SkillSourceType.Certification,
                    SourceCertificationId = certificationId,
                    SourceEmployeeCertificationId = employeeCertificationId,
                    AssessedOn = assessedOn,
                    AssessedBy = assessedBy,
                    ExpiresOn = expiresOn,
                    Status = EmployeeSkillStatus.Active,
                });
            }
            else if (existing.ProficiencyLevel < grant.GrantedProficiency
                     || existing.SourceType != SkillSourceType.Certification)
            {
                existing.ProficiencyLevel = grant.GrantedProficiency;
                existing.SourceType = SkillSourceType.Certification;
                existing.SourceCertificationId = certificationId;
                existing.SourceEmployeeCertificationId = employeeCertificationId;
                existing.AssessedOn = assessedOn;
                existing.AssessedBy = assessedBy;
                existing.ExpiresOn = expiresOn;
                existing.Status = EmployeeSkillStatus.Active;
            }
        }
    }

    public static async Task GrantSkillsFromTrainingAsync(
        CaliberDbContext db,
        int employeeId,
        int trainingProgramId,
        int employeeTrainingId,
        DateOnly? expiresOn,
        DateOnly assessedOn,
        string assessedBy,
        CancellationToken cancellationToken)
    {
        var grants = await db.TrainingProgramSkills
            .AsNoTracking()
            .Where(x => x.TrainingProgramId == trainingProgramId)
            .ToListAsync(cancellationToken);

        foreach (var grant in grants)
        {
            var existing = await db.EmployeeSkills
                .SingleOrDefaultAsync(
                    x => x.EmployeeId == employeeId && x.SkillId == grant.SkillId,
                    cancellationToken);

            if (existing is null)
            {
                db.EmployeeSkills.Add(new EmployeeSkill
                {
                    EmployeeId = employeeId,
                    SkillId = grant.SkillId,
                    ProficiencyLevel = grant.GrantedProficiency,
                    SourceType = SkillSourceType.Training,
                    SourceTrainingProgramId = trainingProgramId,
                    SourceEmployeeTrainingId = employeeTrainingId,
                    AssessedOn = assessedOn,
                    AssessedBy = assessedBy,
                    ExpiresOn = expiresOn,
                    Status = EmployeeSkillStatus.Active,
                });
            }
            else if (existing.ProficiencyLevel < grant.GrantedProficiency
                     || existing.SourceType != SkillSourceType.Training)
            {
                existing.ProficiencyLevel = grant.GrantedProficiency;
                existing.SourceType = SkillSourceType.Training;
                existing.SourceTrainingProgramId = trainingProgramId;
                existing.SourceEmployeeTrainingId = employeeTrainingId;
                existing.AssessedOn = assessedOn;
                existing.AssessedBy = assessedBy;
                existing.ExpiresOn = expiresOn;
                existing.Status = EmployeeSkillStatus.Active;
            }
        }
    }
}
