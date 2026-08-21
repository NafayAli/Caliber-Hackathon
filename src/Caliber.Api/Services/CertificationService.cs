using Caliber.Api.Abstractions;
using Caliber.Api.Common;
using Caliber.Api.Data;
using Caliber.Api.Domain;
using Caliber.Api.Dtos.Catalogues;
using Caliber.Api.Dtos.Employees;
using Caliber.Api.Dtos.Requests;
using Microsoft.EntityFrameworkCore;

namespace Caliber.Api.Services;

public sealed class CertificationService(
    CaliberDbContext db,
    IClock clock,
    ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<CertificationDto>> ListCatalogueAsync(CancellationToken cancellationToken = default) =>
        await db.Certifications
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CertificationDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                Category = c.Category,
                IssuingBody = c.IssuingBody,
                Description = c.Description,
                ValidityMonths = c.ValidityMonths,
                ExpiryWarningDays = c.ExpiryWarningDays,
                RequiresEvidence = c.RequiresEvidence,
                IsActive = c.IsActive,
                GrantedSkills = c.GrantedSkills
                    .Select(g => new GrantedSkillDto
                    {
                        SkillId = g.SkillId,
                        SkillName = g.Skill.Name,
                        GrantedProficiency = g.GrantedProficiency,
                    })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

    public async Task<CertificationDto> GetCatalogueItemAsync(int certificationId, CancellationToken cancellationToken = default)
    {
        var item = await db.Certifications
            .AsNoTracking()
            .Where(c => c.Id == certificationId)
            .Select(c => new CertificationDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                Category = c.Category,
                IssuingBody = c.IssuingBody,
                Description = c.Description,
                ValidityMonths = c.ValidityMonths,
                ExpiryWarningDays = c.ExpiryWarningDays,
                RequiresEvidence = c.RequiresEvidence,
                IsActive = c.IsActive,
                GrantedSkills = c.GrantedSkills
                    .Select(g => new GrantedSkillDto
                    {
                        SkillId = g.SkillId,
                        SkillName = g.Skill.Name,
                        GrantedProficiency = g.GrantedProficiency,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return item ?? throw new NotFoundException("Certification", certificationId);
    }

    public async Task<CertificationDto> CreateAsync(
        CreateCertificationRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanMutateCatalogue();

        if (await db.Certifications.AnyAsync(c => c.Code == request.Code, cancellationToken))
        {
            throw new ConflictException($"A certification with code '{request.Code}' already exists.");
        }

        var certification = new Certification
        {
            Name = request.Name,
            Code = request.Code,
            Category = request.Category,
            IssuingBody = request.IssuingBody,
            Description = request.Description,
            ValidityMonths = request.ValidityMonths,
            ExpiryWarningDays = request.ExpiryWarningDays,
            RequiresEvidence = request.RequiresEvidence,
            IsActive = true,
        };

        db.Certifications.Add(certification);
        await db.SaveChangesAsync(cancellationToken);

        if (request.GrantedSkills.Count > 0)
        {
            await SkillGrantingHelper.ApplyCertificationGrantedSkillsAsync(
                db,
                certification.Id,
                request.GrantedSkills,
                replaceAll: true,
                cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        return await GetCatalogueItemAsync(certification.Id, cancellationToken);
    }

    public async Task<CertificationDto> UpdateAsync(
        int certificationId,
        UpdateCertificationRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanMutateCatalogue();

        var certification = await db.Certifications
            .FirstOrDefaultAsync(c => c.Id == certificationId && c.IsActive, cancellationToken)
            ?? throw new NotFoundException("Certification", certificationId);

        if (request.Code is not null && request.Code != certification.Code
            && await db.Certifications.AnyAsync(c => c.Code == request.Code && c.Id != certificationId, cancellationToken))
        {
            throw new ConflictException($"A certification with code '{request.Code}' already exists.");
        }

        if (request.Name is not null)
        {
            certification.Name = request.Name;
        }

        if (request.Code is not null)
        {
            certification.Code = request.Code;
        }

        if (request.Category is not null)
        {
            certification.Category = request.Category.Value;
        }

        if (request.IssuingBody is not null)
        {
            certification.IssuingBody = request.IssuingBody;
        }

        if (request.Description is not null)
        {
            certification.Description = request.Description;
        }

        if (request.ValidityMonths is not null)
        {
            certification.ValidityMonths = request.ValidityMonths;
        }

        if (request.ExpiryWarningDays is not null)
        {
            certification.ExpiryWarningDays = request.ExpiryWarningDays.Value;
        }

        if (request.RequiresEvidence is not null)
        {
            certification.RequiresEvidence = request.RequiresEvidence.Value;
        }

        await db.SaveChangesAsync(cancellationToken);

        return await GetCatalogueItemAsync(certification.Id, cancellationToken);
    }

    public async Task DeactivateAsync(int certificationId, CancellationToken cancellationToken = default)
    {
        EnsureCanMutateCatalogue();

        var certification = await db.Certifications
            .FirstOrDefaultAsync(c => c.Id == certificationId && c.IsActive, cancellationToken)
            ?? throw new NotFoundException("Certification", certificationId);

        certification.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CertificationDto> SetGrantedSkillsAsync(
        int certificationId,
        SetGrantedSkillsRequest request,
        bool replaceAll,
        CancellationToken cancellationToken = default)
    {
        EnsureCanMutateCatalogue();

        var exists = await db.Certifications
            .AnyAsync(c => c.Id == certificationId && c.IsActive, cancellationToken);

        if (!exists)
        {
            throw new NotFoundException("Certification", certificationId);
        }

        await SkillGrantingHelper.ApplyCertificationGrantedSkillsAsync(
            db,
            certificationId,
            request.Grants,
            replaceAll,
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return await GetCatalogueItemAsync(certificationId, cancellationToken);
    }

    public async Task<EmployeeCertificationDto> AssignAsync(
        int employeeId,
        AssignCertificationRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureEmployeeAccessAsync(employeeId, cancellationToken);

        var certificationExists = await db.Certifications
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.CertificationId && c.IsActive, cancellationToken);

        if (!certificationExists)
        {
            throw new NotFoundException("Certification", request.CertificationId);
        }

        if (await db.EmployeeCertifications.AnyAsync(
                x => x.EmployeeId == employeeId && x.CertificationId == request.CertificationId,
                cancellationToken))
        {
            throw new ConflictException("This certification is already assigned to the employee.");
        }

        var assignment = new EmployeeCertification
        {
            EmployeeId = employeeId,
            CertificationId = request.CertificationId,
            Status = AssignmentStatus.NotStarted,
            Source = AssignmentSource.Direct,
            AssignedOn = clock.Today,
            DueOn = request.DueOn,
            Notes = request.Notes,
        };

        db.EmployeeCertifications.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);

        return await MapAssignmentAsync(assignment.Id, cancellationToken);
    }

    public async Task<EmployeeCertificationDto> RecordAwardAsync(
        int employeeCertificationId,
        RecordAwardRequest request,
        CancellationToken cancellationToken = default)
    {
        var assignment = await db.EmployeeCertifications
            .Include(x => x.Certification)
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == employeeCertificationId, cancellationToken)
            ?? throw new NotFoundException("EmployeeCertification", employeeCertificationId);

        currentUser.EnsureCanAccessEmployee(assignment.EmployeeId, assignment.Employee.LocationId);

        if (assignment.Status == AssignmentStatus.Waived)
        {
            throw new BadRequestException("A waived certification cannot receive an award.");
        }

        db.Entry(assignment).Property(x => x.RowVersion).OriginalValue = request.RowVersion;

        var expiresOn = assignment.Certification.ValidityMonths is int months
            ? request.AwardedOn.AddMonths(months)
            : (DateOnly?)null;

        assignment.Awards.Add(new CertificationAward
        {
            AwardedOn = request.AwardedOn,
            ExpiresOn = expiresOn,
            CertificateNumber = request.CertificateNumber,
            RecordedBy = currentUser.DisplayName,
            RecordedAt = clock.Now,
            Notes = request.Notes,
        });

        assignment.Status = AssignmentStatus.Completed;

        await SkillGrantingHelper.GrantSkillsFromCertificationAsync(
            db,
            assignment.EmployeeId,
            assignment.CertificationId,
            assignment.Id,
            expiresOn,
            request.AwardedOn,
            currentUser.DisplayName,
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return await MapAssignmentAsync(assignment.Id, cancellationToken);
    }

    public async Task<EmployeeCertificationDto> WaiveAsync(
        int employeeCertificationId,
        WaiveAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var assignment = await db.EmployeeCertifications
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == employeeCertificationId, cancellationToken)
            ?? throw new NotFoundException("EmployeeCertification", employeeCertificationId);

        currentUser.EnsureCanAccessEmployee(assignment.EmployeeId, assignment.Employee.LocationId);
        EnsureCanMutateCatalogue();

        db.Entry(assignment).Property(x => x.RowVersion).OriginalValue = request.RowVersion;

        assignment.Status = AssignmentStatus.Waived;
        assignment.Notes = request.Reason;

        await db.SaveChangesAsync(cancellationToken);

        return await MapAssignmentAsync(assignment.Id, cancellationToken);
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
            throw new ForbiddenException("Only managers and administrators may modify the certification catalogue or waive requirements.");
        }
    }

    private async Task<EmployeeCertificationDto> MapAssignmentAsync(
        int employeeCertificationId,
        CancellationToken cancellationToken)
    {
        var row = await db.EmployeeCertifications
            .AsNoTracking()
            .Where(x => x.Id == employeeCertificationId)
            .Select(x => new
            {
                Assignment = x,
                Certification = x.Certification,
                LatestAward = x.Awards.OrderByDescending(a => a.AwardedOn).FirstOrDefault(),
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("EmployeeCertification", employeeCertificationId);

        var latestAward = row.LatestAward;
        var isCompleted = row.Assignment.Status == AssignmentStatus.Completed
                          || row.Assignment.Status == AssignmentStatus.Waived
                          || latestAward is not null;

        var readinessStatus = ReadinessService.ComputeStatus(
            row.Assignment.Status,
            isCompleted,
            latestAward?.ExpiresOn,
            row.Assignment.DueOn,
            row.Certification.ExpiryWarningDays,
            clock.Today);

        return new EmployeeCertificationDto
        {
            Id = row.Assignment.Id,
            CertificationId = row.Certification.Id,
            CertificationName = row.Certification.Name,
            CertificationCode = row.Certification.Code,
            Status = row.Assignment.Status,
            Source = row.Assignment.Source,
            AssignedOn = row.Assignment.AssignedOn,
            DueOn = row.Assignment.DueOn,
            Notes = row.Assignment.Notes,
            ReadinessStatus = readinessStatus,
            RowVersion = row.Assignment.RowVersion ?? [],
            LatestAward = latestAward is null
                ? null
                : new CertificationAwardDto
                {
                    Id = latestAward.Id,
                    AwardedOn = latestAward.AwardedOn,
                    ExpiresOn = latestAward.ExpiresOn,
                    CertificateNumber = latestAward.CertificateNumber,
                    RecordedBy = latestAward.RecordedBy,
                    RecordedAt = latestAward.RecordedAt,
                    Notes = latestAward.Notes,
                },
        };
    }
}
