using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceBook.Application.DTOs.Admin;
using SpaceBook.Application.Interfaces;
using SpaceBook.Application.DTOs.Employee;

namespace SpaceBook.API.Controllers;

[ApiController]
[Route("api/admin/notifications")]
[Authorize(Roles = "Admin")]
public class AdminNotificationController : ControllerBase
{
    private readonly INotificationRepository _notificationRepository;

    public AdminNotificationController(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    // GET: api/admin/notifications
    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        try
        {
            // Fetch only admin-specific request records instead of all notifications
            var notifications = await _notificationRepository.GetAdminNotificationsAsync();

            // Perform mapping in memory to prevent EF Core expression tree translation errors
            var response = notifications.Select(n =>
            {
                var message = n.Message ?? string.Empty;

                // Default fallback title for admin view
                string title = string.IsNullOrEmpty(n.Title) ? "Booking Request" : n.Title;

                // Ensure n.CreatedOn / CreatedAt is valid
                var createdDate = n.CreatedOn == default ? DateTime.UtcNow : n.CreatedOn;

                return new NotificationDto
                {
                    NotificationId = n.NotificationId,
                    Title = title,
                    Message = message,
                    IsRead = n.IsRead,
                    CreatedOn = createdDate,
                    TimeAgo = string.IsNullOrEmpty(n.TimeAgo) ? GetTimeAgo(createdDate) : n.TimeAgo
                };
            })
            .OrderByDescending(x => x.NotificationId) // Ensure newest notifications appear first
            .ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Something went wrong.",
                error = ex.Message
            });
        }
    }

    // PATCH: api/admin/notifications/read-all
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            // Marks unread requests as read in the database
            await _notificationRepository.MarkAllAsReadAsync(0);

            return Ok(new { message = "All admin notifications marked as read." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Something went wrong.",
                error = ex.Message
            });
        }
    }

    private static string GetTimeAgo(DateTime dateTime)
    {
        // Handle unspecified DateTime kinds (common with PostgreSQL timestamps without time zone)
        var utcDate = dateTime.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc) 
            : dateTime.ToUniversalTime();

        var timeSpan = DateTime.UtcNow - utcDate;

        if (timeSpan.TotalSeconds < 0 || timeSpan.TotalSeconds < 60)
            return "Just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes}m ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours}h ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays}d ago";

        return dateTime.ToString("MMM dd, yyyy");
    }
}