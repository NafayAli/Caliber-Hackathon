using Caliber.Api.Abstractions;
using Caliber.Api.Common;
using Caliber.Api.Data;
using Caliber.Api.Domain;
using Caliber.Api.Dtos.Notifications;
using Caliber.Api.Dtos.Requests;
using Microsoft.EntityFrameworkCore;

namespace Caliber.Api.Services;

public sealed class NotificationService(
    CaliberDbContext db,
    IClock clock,
    ICurrentUser currentUser)
{
    public async Task<NotificationSummaryDto> GetSummaryAsync(
        int take = 30,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        var employeeId = currentUser.EmployeeId;

        var unreadCount = await db.Notifications
            .AsNoTracking()
            .CountAsync(n => n.RecipientEmployeeId == employeeId && !n.IsRead, cancellationToken);

        var items = await db.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientEmployeeId == employeeId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Kind = n.Kind,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                RelatedEmployeeId = n.RelatedEmployeeId,
                RelatedKind = n.RelatedKind,
                RelatedAssignmentId = n.RelatedAssignmentId,
                RenewalRequestId = n.RenewalRequestId,
                CreatedByName = n.CreatedBy != null ? n.CreatedBy.FirstName + " " + n.CreatedBy.LastName : null,
            })
            .ToListAsync(cancellationToken);

        return new NotificationSummaryDto
        {
            UnreadCount = unreadCount,
            Items = items,
        };
    }

    public async Task MarkReadAsync(int notificationId, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        var notification = await db.Notifications
            .FirstOrDefaultAsync(
                n => n.Id == notificationId && n.RecipientEmployeeId == currentUser.EmployeeId,
                cancellationToken)
            ?? throw new NotFoundException("Notification", notificationId);

        notification.IsRead = true;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllReadAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        var employeeId = currentUser.EmployeeId;
        var unread = await db.Notifications
            .Where(n => n.RecipientEmployeeId == employeeId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var item in unread)
        {
            item.IsRead = true;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> BroadcastAnnouncementAsync(
        BroadcastAnnouncementRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureManagerOrAdmin();

        var recipients = await GetScopedActiveEmployeesAsync(request.LocationId, cancellationToken);
        return await CreateForEmployeesAsync(
            recipients,
            NotificationKind.Announcement,
            request.Title.Trim(),
            request.Message.Trim(),
            currentUser.EmployeeId,
            cancellationToken);
    }

    public async Task<int> NotifyEmployeesAsync(
        NotifyEmployeesRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureManagerOrAdmin();

        if (request.EmployeeIds.Count == 0)
        {
            throw new BadRequestException("At least one employee must be selected.");
        }

        var scopedIds = await GetScopedActiveEmployeeIdsAsync(cancellationToken);
        var allowed = request.EmployeeIds.Where(scopedIds.Contains).Distinct().ToList();

        if (allowed.Count == 0)
        {
            throw new ForbiddenException("None of the selected employees are in your scope.");
        }

        return await CreateForEmployeesAsync(
            allowed,
            request.Kind,
            request.Title.Trim(),
            request.Message.Trim(),
            currentUser.EmployeeId,
            cancellationToken);
    }

    public async Task NotifyManagersOfAcknowledgementAsync(
        int employeeId,
        string employeeName,
        string trainingName,
        int assignmentId,
        CancellationToken cancellationToken = default)
    {
        var employee = await db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken)
            ?? throw new NotFoundException("Employee", employeeId);

        var managers = await db.Employees
            .AsNoTracking()
            .Where(e => e.IsActive
                        && (e.AccessLevel == AccessLevel.Manager || e.AccessLevel == AccessLevel.Admin)
                        && (e.AccessLevel == AccessLevel.Admin || e.LocationId == employee.LocationId))
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        var title = "Training acknowledged";
        var message = $"{employeeName} acknowledged completion of {trainingName}.";

        await CreateForEmployeesAsync(
            managers,
            NotificationKind.Acknowledgement,
            title,
            message,
            employeeId,
            cancellationToken,
            relatedEmployeeId: employeeId,
            relatedKind: RequirementKind.Training,
            relatedAssignmentId: assignmentId);
    }

    public async Task NotifyManagersOfRenewalRequestAsync(
        RenewalRequest request,
        string employeeName,
        string requirementName,
        CancellationToken cancellationToken = default)
    {
        var employee = request.Employee
            ?? await db.Employees.AsNoTracking().FirstAsync(e => e.Id == request.EmployeeId, cancellationToken);

        var managers = await db.Employees
            .AsNoTracking()
            .Where(e => e.IsActive
                        && (e.AccessLevel == AccessLevel.Manager || e.AccessLevel == AccessLevel.Admin)
                        && (e.AccessLevel == AccessLevel.Admin || e.LocationId == employee.LocationId))
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        await CreateForEmployeesAsync(
            managers,
            NotificationKind.RenewalRequest,
            "Renewal request",
            $"{employeeName} requested renewal for {requirementName}.",
            request.EmployeeId,
            cancellationToken,
            relatedEmployeeId: request.EmployeeId,
            relatedKind: request.Kind,
            relatedAssignmentId: request.AssignmentId,
            renewalRequestId: request.Id);
    }

    public async Task NotifyEmployeeAsync(
        int employeeId,
        NotificationKind kind,
        string title,
        string message,
        int? createdByEmployeeId = null,
        int? renewalRequestId = null,
        CancellationToken cancellationToken = default) =>
        await CreateForEmployeesAsync(
            [employeeId],
            kind,
            title,
            message,
            createdByEmployeeId,
            cancellationToken,
            relatedEmployeeId: employeeId,
            renewalRequestId: renewalRequestId);

    private async Task<int> CreateForEmployeesAsync(
        IReadOnlyList<int> employeeIds,
        NotificationKind kind,
        string title,
        string message,
        int? createdByEmployeeId,
        CancellationToken cancellationToken,
        int? relatedEmployeeId = null,
        RequirementKind? relatedKind = null,
        int? relatedAssignmentId = null,
        int? renewalRequestId = null)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
        {
            throw new BadRequestException("Title and message are required.");
        }

        var now = clock.Now;
        foreach (var recipientId in employeeIds.Distinct())
        {
            db.Notifications.Add(new Notification
            {
                RecipientEmployeeId = recipientId,
                CreatedByEmployeeId = createdByEmployeeId,
                Kind = kind,
                Title = title,
                Message = message,
                IsRead = false,
                CreatedAt = now,
                RelatedEmployeeId = relatedEmployeeId,
                RelatedKind = relatedKind,
                RelatedAssignmentId = relatedAssignmentId,
                RenewalRequestId = renewalRequestId,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return employeeIds.Count;
    }

    private async Task<List<int>> GetScopedActiveEmployeeIdsAsync(CancellationToken cancellationToken)
    {
        var query = db.Employees.AsNoTracking().Where(e => e.IsActive);

        if (currentUser.AccessLevel == AccessLevel.Manager)
        {
            query = query.Where(e => e.LocationId == currentUser.LocationId);
        }
        else if (currentUser.AccessLevel != AccessLevel.Admin)
        {
            query = query.Where(e => e.Id == currentUser.EmployeeId);
        }

        return await query.Select(e => e.Id).ToListAsync(cancellationToken);
    }

    private async Task<List<int>> GetScopedActiveEmployeesAsync(
        int? locationId,
        CancellationToken cancellationToken)
    {
        var query = db.Employees.AsNoTracking().Where(e => e.IsActive);

        if (currentUser.AccessLevel == AccessLevel.Manager)
        {
            query = query.Where(e => e.LocationId == currentUser.LocationId);
        }
        else if (currentUser.AccessLevel != AccessLevel.Admin)
        {
            throw new ForbiddenException("Only managers and administrators may broadcast announcements.");
        }

        if (locationId is not null)
        {
            query = query.Where(e => e.LocationId == locationId);
        }

        return await query.Select(e => e.Id).ToListAsync(cancellationToken);
    }

    private void EnsureAuthenticated()
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new ForbiddenException("You must be signed in.");
        }
    }

    private void EnsureManagerOrAdmin()
    {
        EnsureAuthenticated();
        if (currentUser.AccessLevel is not AccessLevel.Manager and not AccessLevel.Admin)
        {
            throw new ForbiddenException("Only managers and administrators may send notifications.");
        }
    }
}
