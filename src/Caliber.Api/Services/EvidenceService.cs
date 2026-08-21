using Caliber.Api.Abstractions;
using Caliber.Api.Common;
using Caliber.Api.Data;
using Caliber.Api.Domain;
using Caliber.Api.Dtos.Evidence;
using Caliber.Api.Storage;
using Microsoft.EntityFrameworkCore;

namespace Caliber.Api.Services;

public sealed class EvidenceService(
    CaliberDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    IEvidenceStorage storage)
{
    public async Task<EvidenceDto> UploadAsync(
        EvidenceUploadRequest request,
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        await EnsureEmployeeAccessAsync(request.EmployeeId, cancellationToken);
        await EnsureAssignmentLinkAsync(request, cancellationToken);

        var stored = await storage.SaveAsync(content, originalFileName, contentType, cancellationToken);

        var evidence = new Evidence
        {
            EmployeeId = request.EmployeeId,
            EvidenceType = request.EvidenceType,
            FileName = Path.GetFileName(originalFileName),
            StoredFileName = stored.StoredFileName,
            ContentType = contentType,
            SizeBytes = stored.SizeBytes,
            UploadedOn = clock.Now,
            UploadedBy = currentUser.DisplayName,
            EmployeeCertificationId = request.EmployeeCertificationId,
            EmployeeTrainingId = request.EmployeeTrainingId,
            EmployeeSkillId = request.EmployeeSkillId,
        };

        db.Evidence.Add(evidence);
        await db.SaveChangesAsync(cancellationToken);

        return Map(evidence);
    }

    public async Task<(Stream Content, EvidenceDto Metadata)> OpenContentAsync(
        int evidenceId,
        CancellationToken cancellationToken = default)
    {
        var evidence = await LoadEvidenceAsync(evidenceId, cancellationToken);
        var stream = await storage.OpenReadAsync(evidence.StoredFileName, cancellationToken);
        return (stream, Map(evidence));
    }

    public async Task<EvidenceDto> VerifyAsync(
        int evidenceId,
        VerifyEvidenceRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanVerify();

        var evidence = await db.Evidence
            .FirstOrDefaultAsync(e => e.Id == evidenceId, cancellationToken)
            ?? throw new NotFoundException("Evidence", evidenceId);

        var employee = await db.Employees
            .AsNoTracking()
            .Where(e => e.Id == evidence.EmployeeId)
            .Select(e => new { e.Id, e.LocationId })
            .FirstAsync(cancellationToken);

        currentUser.EnsureCanAccessEmployee(employee.Id, employee.LocationId);

        evidence.IsVerified = true;
        evidence.VerifiedBy = currentUser.DisplayName;
        evidence.VerifiedOn = clock.Now;

        await db.SaveChangesAsync(cancellationToken);

        return Map(evidence);
    }

    public async Task DeleteAsync(int evidenceId, CancellationToken cancellationToken = default)
    {
        var evidence = await db.Evidence
            .FirstOrDefaultAsync(e => e.Id == evidenceId, cancellationToken)
            ?? throw new NotFoundException("Evidence", evidenceId);

        var employee = await db.Employees
            .AsNoTracking()
            .Where(e => e.Id == evidence.EmployeeId)
            .Select(e => new { e.Id, e.LocationId })
            .FirstAsync(cancellationToken);

        currentUser.EnsureCanAccessEmployee(employee.Id, employee.LocationId);

        await storage.DeleteAsync(evidence.StoredFileName, cancellationToken);
        db.Evidence.Remove(evidence);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Evidence> LoadEvidenceAsync(int evidenceId, CancellationToken cancellationToken)
    {
        var evidence = await db.Evidence
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == evidenceId, cancellationToken)
            ?? throw new NotFoundException("Evidence", evidenceId);

        var employee = await db.Employees
            .AsNoTracking()
            .Where(e => e.Id == evidence.EmployeeId)
            .Select(e => new { e.Id, e.LocationId })
            .FirstAsync(cancellationToken);

        currentUser.EnsureCanAccessEmployee(employee.Id, employee.LocationId);
        return evidence;
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

    private async Task EnsureAssignmentLinkAsync(
        EvidenceUploadRequest request,
        CancellationToken cancellationToken)
    {
        if (request.EvidenceType == EvidenceType.General)
        {
            return;
        }

        if (request.EmployeeCertificationId is int certificationAssignmentId)
        {
            var valid = await db.EmployeeCertifications.AnyAsync(
                x => x.Id == certificationAssignmentId && x.EmployeeId == request.EmployeeId,
                cancellationToken);

            if (!valid)
            {
                throw new BadRequestException("The certification assignment does not belong to this employee.");
            }

            return;
        }

        if (request.EmployeeTrainingId is int trainingAssignmentId)
        {
            var valid = await db.EmployeeTrainings.AnyAsync(
                x => x.Id == trainingAssignmentId && x.EmployeeId == request.EmployeeId,
                cancellationToken);

            if (!valid)
            {
                throw new BadRequestException("The training assignment does not belong to this employee.");
            }

            return;
        }

        if (request.EmployeeSkillId is int skillAssignmentId)
        {
            var valid = await db.EmployeeSkills.AnyAsync(
                x => x.Id == skillAssignmentId && x.EmployeeId == request.EmployeeId,
                cancellationToken);

            if (!valid)
            {
                throw new BadRequestException("The skill assignment does not belong to this employee.");
            }
        }
    }

    private void EnsureCanVerify()
    {
        if (currentUser.AccessLevel == AccessLevel.Technician)
        {
            throw new ForbiddenException("Only managers and administrators may verify evidence.");
        }
    }

    private static EvidenceDto Map(Evidence evidence) =>
        new()
        {
            Id = evidence.Id,
            EmployeeId = evidence.EmployeeId,
            EvidenceType = evidence.EvidenceType,
            FileName = evidence.FileName,
            ContentType = evidence.ContentType,
            SizeBytes = evidence.SizeBytes,
            UploadedOn = evidence.UploadedOn,
            UploadedBy = evidence.UploadedBy,
            IsVerified = evidence.IsVerified,
            VerifiedBy = evidence.VerifiedBy,
            VerifiedOn = evidence.VerifiedOn,
            EmployeeCertificationId = evidence.EmployeeCertificationId,
            EmployeeTrainingId = evidence.EmployeeTrainingId,
            EmployeeSkillId = evidence.EmployeeSkillId,
        };
}
