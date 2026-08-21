using Caliber.Api.Abstractions;
using Caliber.Api.Common;
using Caliber.Api.Data;
using Caliber.Api.Domain;
using Caliber.Api.Dtos.Auth;
using Caliber.Api.Dtos.Common;
using Caliber.Api.Dtos.Employees;
using Caliber.Api.Dtos.Evidence;
using Caliber.Api.Dtos.Personas;
using Caliber.Api.Dtos.Requests;
using Caliber.Api.Storage;
using Microsoft.EntityFrameworkCore;

namespace Caliber.Api.Services;

public sealed class EmployeeService(
    CaliberDbContext db,
    ICurrentUser currentUser,
    ReadinessService readiness,
    IClock clock,
    LocalFileAvatarStorage avatarStorage)
{
    public async Task<PagedResult<EmployeeListItemDto>> ListAsync(
        EmployeeListQuery query,
        CancellationToken cancellationToken = default)
    {
        var employeesQuery = db.Employees
            .AsNoTracking()
            .Where(e => e.IsActive);

        employeesQuery = ApplyScope(employeesQuery);

        if (query.LocationId is int locationId)
        {
            employeesQuery = employeesQuery.Where(e => e.LocationId == locationId);
        }

        if (query.JobRoleId is int jobRoleId)
        {
            employeesQuery = employeesQuery.Where(e => e.JobRoleId == jobRoleId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            employeesQuery = employeesQuery.Where(e =>
                e.FirstName.Contains(term)
                || e.LastName.Contains(term)
                || e.Email.Contains(term)
                || (e.ExternalEmployeeNo != null && e.ExternalEmployeeNo.Contains(term)));
        }

        var totalCount = await employeesQuery.CountAsync(cancellationToken);

        var page = await employeesQuery
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Skip(query.Offset)
            .Take(query.Limit)
            .Select(e => new EmployeeListItemDto
            {
                Id = e.Id,
                FullName = e.FirstName + " " + e.LastName,
                Email = e.Email,
                JobRole = e.JobRole.Name,
                Location = e.Location.Name,
            })
            .ToListAsync(cancellationToken);

        if (page.Count == 0)
        {
            return new PagedResult<EmployeeListItemDto>
            {
                Items = page,
                TotalCount = totalCount,
                Offset = query.Offset,
                Limit = query.Limit,
            };
        }

        var employeeIds = page.Select(e => e.Id).ToArray();
        var requirementsByEmployee = await readiness.GetRequirementsForEmployeesAsync(employeeIds, cancellationToken);

        var items = page.Select(employee =>
        {
            if (!requirementsByEmployee.TryGetValue(employee.Id, out var requirements) || requirements.Count == 0)
            {
                return employee with
                {
                    ReadinessPercent = 100m,
                    WorstStatus = ReadinessStatus.Compliant,
                };
            }

            if (query.Status is ReadinessStatus filterStatus
                && !requirements.Any(r => r.Status == filterStatus))
            {
                return null;
            }

            var worst = requirements
                .Select(r => r.Status)
                .OrderBy(GetStatusPriority)
                .First();

            return employee with
            {
                ReadinessPercent = ReadinessService.ComputeReadinessPercent(requirements),
                WorstStatus = worst,
            };
        }).Where(item => item is not null).Cast<EmployeeListItemDto>().ToList();

        return new PagedResult<EmployeeListItemDto>
        {
            Items = items,
            TotalCount = query.Status is null ? totalCount : items.Count,
            Offset = query.Offset,
            Limit = query.Limit,
        };
    }

    public async Task<EmployeeProfileDto> GetProfileAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await db.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId && e.IsActive)
            .Select(e => new
            {
                e.Id,
                e.FirstName,
                e.LastName,
                e.Email,
                e.ExternalEmployeeNo,
                e.JobRoleId,
                JobRole = e.JobRole.Name,
                e.LocationId,
                Location = e.Location.Name,
                e.HireDate,
                e.AccessLevel,
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Employee", employeeId);

        currentUser.EnsureCanAccessEmployee(employee.Id, employee.LocationId);

        var requirements = await readiness.GetRequirementsForEmployeeAsync(employeeId, cancellationToken);

        var today = clock.Today;

        var skills = await db.EmployeeSkills
            .AsNoTracking()
            .Where(s => s.EmployeeId == employeeId
                        && s.Status == EmployeeSkillStatus.Active
                        && (s.ExpiresOn == null || s.ExpiresOn >= today))
            .OrderBy(s => s.Skill.Name)
            .Select(s => new EmployeeSkillDto
            {
                Id = s.Id,
                SkillId = s.SkillId,
                SkillName = s.Skill.Name,
                Category = s.Skill.Category,
                ProficiencyLevel = s.ProficiencyLevel,
                SourceType = s.SourceType,
                SourceCertificationId = s.SourceCertificationId,
                SourceTrainingProgramId = s.SourceTrainingProgramId,
                AssessedOn = s.AssessedOn,
                AssessedBy = s.AssessedBy,
                Notes = s.Notes,
                ExpiresOn = s.ExpiresOn,
                Status = s.Status,
                RowVersion = s.RowVersion == null ? Array.Empty<byte>() : s.RowVersion,
            })
            .ToListAsync(cancellationToken);

        var evidence = await db.Evidence
            .AsNoTracking()
            .Where(ev => ev.EmployeeId == employeeId)
            .OrderByDescending(ev => ev.UploadedOn)
            .Select(ev => new EvidenceDto
            {
                Id = ev.Id,
                EmployeeId = ev.EmployeeId,
                EvidenceType = ev.EvidenceType,
                FileName = ev.FileName,
                ContentType = ev.ContentType,
                SizeBytes = ev.SizeBytes,
                UploadedOn = ev.UploadedOn,
                UploadedBy = ev.UploadedBy,
                IsVerified = ev.IsVerified,
                VerifiedBy = ev.VerifiedBy,
                VerifiedOn = ev.VerifiedOn,
                EmployeeCertificationId = ev.EmployeeCertificationId,
                EmployeeTrainingId = ev.EmployeeTrainingId,
                EmployeeSkillId = ev.EmployeeSkillId,
            })
            .ToListAsync(cancellationToken);

        return new EmployeeProfileDto
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            FullName = $"{employee.FirstName} {employee.LastName}",
            Email = employee.Email,
            ExternalEmployeeNo = employee.ExternalEmployeeNo,
            JobRole = employee.JobRole,
            JobRoleId = employee.JobRoleId,
            Location = employee.Location,
            LocationId = employee.LocationId,
            HireDate = employee.HireDate,
            AccessLevel = employee.AccessLevel,
            ReadinessPercent = ReadinessService.ComputeReadinessPercent(requirements),
            Requirements = requirements,
            Skills = skills,
            Evidence = evidence,
        };
    }

    public async Task<IReadOnlyList<RequirementStatusDto>> GetRequirementsAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var employee = await db.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId && e.IsActive)
            .Select(e => new { e.Id, e.LocationId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Employee", employeeId);

        currentUser.EnsureCanAccessEmployee(employee.Id, employee.LocationId);

        return await readiness.GetRequirementsForEmployeeAsync(employeeId, cancellationToken);
    }

    public async Task<EmployeeProfileDto> CreateAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanManageEmployees();

        if (currentUser.AccessLevel == AccessLevel.Manager && request.LocationId != currentUser.LocationId)
        {
            throw new ForbiddenException("Managers may only create employees at their own location.");
        }

        if (currentUser.AccessLevel == AccessLevel.Manager && request.AccessLevel == AccessLevel.Admin)
        {
            throw new ForbiddenException("Managers may not create administrator accounts.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await db.Employees.AnyAsync(e => e.Email == normalizedEmail, cancellationToken)
            || await db.UserAccounts.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
        {
            throw new ConflictException("An employee or account with this email already exists.");
        }

        await EnsureOrganisationReferencesAsync(request.LocationId, request.JobRoleId, cancellationToken);

        var employee = new Employee
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = normalizedEmail,
            ExternalEmployeeNo = request.ExternalEmployeeNo?.Trim(),
            LocationId = request.LocationId,
            JobRoleId = request.JobRoleId,
            HireDate = request.HireDate ?? clock.Today,
            AccessLevel = request.AccessLevel,
            IsActive = true,
        };

        var account = new UserAccount
        {
            Email = normalizedEmail,
            PasswordHash = AuthService.HashPassword(request.Password),
            CreatedAt = clock.Now,
            Employee = employee,
        };

        db.Employees.Add(employee);
        db.UserAccounts.Add(account);
        await db.SaveChangesAsync(cancellationToken);

        return await GetProfileAsync(employee.Id, cancellationToken);
    }

    public async Task<EmployeeProfileDto> UpdateAsync(
        int employeeId,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanManageEmployees();

        var employee = await db.Employees
            .Include(e => e.UserAccount)
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken)
            ?? throw new NotFoundException("Employee", employeeId);

        currentUser.EnsureCanAccessEmployee(employee.Id, employee.LocationId);

        if (currentUser.AccessLevel == AccessLevel.Manager)
        {
            if (request.LocationId is int locationId && locationId != currentUser.LocationId)
            {
                throw new ForbiddenException("Managers may not move employees to another location.");
            }

            if (request.AccessLevel == AccessLevel.Admin)
            {
                throw new ForbiddenException("Managers may not grant administrator access.");
            }
        }

        if (request.Email is not null)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            if (normalizedEmail != employee.Email
                && (await db.Employees.AnyAsync(e => e.Email == normalizedEmail && e.Id != employeeId, cancellationToken)
                    || await db.UserAccounts.AnyAsync(u => u.Email == normalizedEmail && u.EmployeeId != employeeId, cancellationToken)))
            {
                throw new ConflictException("An employee or account with this email already exists.");
            }

            employee.Email = normalizedEmail;
            if (employee.UserAccount is not null)
            {
                employee.UserAccount.Email = normalizedEmail;
            }
        }

        if (request.FirstName is not null)
        {
            employee.FirstName = request.FirstName.Trim();
        }

        if (request.LastName is not null)
        {
            employee.LastName = request.LastName.Trim();
        }

        if (request.ExternalEmployeeNo is not null)
        {
            employee.ExternalEmployeeNo = string.IsNullOrWhiteSpace(request.ExternalEmployeeNo)
                ? null
                : request.ExternalEmployeeNo.Trim();
        }

        if (request.JobRoleId is int jobRoleId)
        {
            await EnsureOrganisationReferencesAsync(employee.LocationId, jobRoleId, cancellationToken);
            employee.JobRoleId = jobRoleId;
        }

        if (request.LocationId is int newLocationId)
        {
            await EnsureOrganisationReferencesAsync(newLocationId, employee.JobRoleId, cancellationToken);
            employee.LocationId = newLocationId;
        }

        if (request.HireDate is not null)
        {
            employee.HireDate = request.HireDate.Value;
        }

        if (request.AccessLevel is not null)
        {
            employee.AccessLevel = request.AccessLevel.Value;
        }

        if (request.IsActive is not null)
        {
            employee.IsActive = request.IsActive.Value;
        }

        await db.SaveChangesAsync(cancellationToken);

        return await GetProfileAsync(employee.Id, cancellationToken);
    }

    public async Task<AuthUserDto> UpdateProfileAsync(
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new ForbiddenException();
        }

        var employee = await db.Employees
            .Include(e => e.Location)
            .Include(e => e.JobRole)
            .Include(e => e.UserAccount)
            .FirstOrDefaultAsync(e => e.Id == currentUser.EmployeeId && e.IsActive, cancellationToken)
            ?? throw new NotFoundException("Employee", currentUser.EmployeeId);

        if (request.FirstName is not null)
        {
            employee.FirstName = request.FirstName.Trim();
        }

        if (request.LastName is not null)
        {
            employee.LastName = request.LastName.Trim();
        }

        if (request.Phone is not null)
        {
            employee.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        }

        if (request.Bio is not null)
        {
            employee.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();
        }

        await db.SaveChangesAsync(cancellationToken);

        return new AuthUserDto
        {
            EmployeeId = employee.Id,
            Email = employee.UserAccount?.Email ?? employee.Email,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            DisplayName = employee.FullName,
            AccessLevel = employee.AccessLevel,
            LocationId = employee.LocationId,
            LocationName = employee.Location.Name,
            JobRoleName = employee.JobRole.Name,
            Phone = employee.Phone,
            Bio = employee.Bio,
            AvatarUrl = employee.AvatarFileName is not null ? $"/api/me/avatar/{employee.Id}" : null,
        };
    }

    public async Task<AuthUserDto> UploadAvatarAsync(
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new ForbiddenException();
        }

        var employee = await db.Employees
            .Include(e => e.Location)
            .Include(e => e.JobRole)
            .Include(e => e.UserAccount)
            .FirstOrDefaultAsync(e => e.Id == currentUser.EmployeeId && e.IsActive, cancellationToken)
            ?? throw new NotFoundException("Employee", currentUser.EmployeeId);

        var stored = await avatarStorage.SaveAsync(content, originalFileName, contentType, cancellationToken);
        var previousFile = employee.AvatarFileName;

        employee.AvatarFileName = stored.StoredFileName;
        await db.SaveChangesAsync(cancellationToken);
        await avatarStorage.DeleteAsync(previousFile, cancellationToken);

        return new AuthUserDto
        {
            EmployeeId = employee.Id,
            Email = employee.UserAccount?.Email ?? employee.Email,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            DisplayName = employee.FullName,
            AccessLevel = employee.AccessLevel,
            LocationId = employee.LocationId,
            LocationName = employee.Location.Name,
            JobRoleName = employee.JobRole.Name,
            Phone = employee.Phone,
            Bio = employee.Bio,
            AvatarUrl = $"/api/me/avatar/{employee.Id}",
        };
    }

    public async Task<(Stream Content, string ContentType)> OpenAvatarAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var employee = await db.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId && e.IsActive)
            .Select(e => new { e.Id, e.LocationId, e.AvatarFileName })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Employee", employeeId);

        currentUser.EnsureCanAccessEmployee(employee.Id, employee.LocationId);

        if (employee.AvatarFileName is null)
        {
            throw new NotFoundException("Avatar", employeeId);
        }

        var stream = await avatarStorage.OpenReadAsync(employee.AvatarFileName, cancellationToken);
        var contentType = avatarStorage.ResolveContentType(employee.AvatarFileName);
        return (stream, contentType);
    }

    public async Task<IReadOnlyList<PersonaDto>> ListPersonasAsync(CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new ForbiddenException();
        }

        if (currentUser.AccessLevel != AccessLevel.Admin)
        {
            throw new ForbiddenException("Only administrators may switch personas.");
        }

        var query = db.Employees.AsNoTracking().Where(e => e.IsActive);

        return await query
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Select(e => new PersonaDto
            {
                Id = e.Id,
                DisplayName = e.FirstName + " " + e.LastName,
                AccessLevel = e.AccessLevel,
                JobRole = e.JobRole.Name,
                Location = e.Location.Name,
            })
            .ToListAsync(cancellationToken);
    }

    private void EnsureCanManageEmployees()
    {
        if (currentUser.AccessLevel == AccessLevel.Technician)
        {
            throw new ForbiddenException("Only managers and administrators may manage employees.");
        }
    }

    private async Task EnsureOrganisationReferencesAsync(
        int locationId,
        int jobRoleId,
        CancellationToken cancellationToken)
    {
        if (!await db.Locations.AnyAsync(l => l.Id == locationId, cancellationToken))
        {
            throw new BadRequestException("The selected location is not valid.");
        }

        if (!await db.JobRoles.AnyAsync(r => r.Id == jobRoleId, cancellationToken))
        {
            throw new BadRequestException("The selected job role is not valid.");
        }
    }

    private IQueryable<Employee> ApplyScope(IQueryable<Employee> query) =>
        currentUser.AccessLevel switch
        {
            AccessLevel.Admin => query,
            AccessLevel.Manager => query.Where(e => e.LocationId == currentUser.LocationId),
            AccessLevel.Technician => query.Where(e => e.Id == currentUser.EmployeeId),
            _ => query.Where(_ => false),
        };

    private static int GetStatusPriority(ReadinessStatus status) =>
        status switch
        {
            ReadinessStatus.Expired => 0,
            ReadinessStatus.Overdue => 1,
            ReadinessStatus.Missing => 2,
            ReadinessStatus.InProgress => 3,
            ReadinessStatus.ExpiringSoon => 4,
            ReadinessStatus.Compliant => 5,
            ReadinessStatus.Waived => 6,
            _ => 7,
        };
}
