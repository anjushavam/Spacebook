using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceBook.Application.DTOs.Employee;
using SpaceBook.Application.Interfaces;
using SpaceBook.Application.DTOs.Admin; // <--- Add if NotificationDto is shared with Admin DTOs
// <--- Add if it's located inside Employee DTOs

namespace SpaceBook.API.Controllers;

[ApiController]
[Route("api/employee/notifications")]
[Authorize(Roles = "Employee")]
public class EmployeeNotificationController : ControllerBase
{
    private readonly INotificationRepository _notificationRepository;

    public EmployeeNotificationController(
        INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    // GET: api/employee/notifications
    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        try
        {
            var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (employeeIdClaim == null || !int.TryParse(employeeIdClaim.Value, out int employeeId))
            {
                return Unauthorized(new
                {
                    message = "Invalid token."
                });
            }

            var notifications = await _notificationRepository
                .GetEmployeeNotificationsAsync(employeeId);

            // Perform in-memory transformation to ensure clean title & timeAgo format
            var response = notifications.Select(n =>
            {
                var message = n.Message ?? string.Empty;

                // Default fallback title
                string title = "Notification";

                // 1. Check for Missed Check-in
                if (message.Contains("missed check-in", StringComparison.OrdinalIgnoreCase) ||
                    message.Contains("missed", StringComparison.OrdinalIgnoreCase))
                {
                    title = "Missed Check-in";
                }
                // 2. Check for Approval
                else if (message.Contains("approved", StringComparison.OrdinalIgnoreCase) ||
                         message.Contains("confirmed", StringComparison.OrdinalIgnoreCase))
                {
                    title = "Booking Approved";
                }
                // 3. Check for Rejection / Cancellation
                else if (message.Contains("rejected", StringComparison.OrdinalIgnoreCase) ||
                         message.Contains("cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    title = "Booking Cancelled";
                }

                // Ensure valid CreatedAt date
                var createdDate = n.CreatedAt == default ? DateTime.UtcNow : n.CreatedAt;

                return new NotificationDto
                {
                    NotificationId = n.NotificationId,
                    Title = title,
                    Message = message,
                    IsRead = n.IsRead,
                    CreatedOn = createdDate,
                    TimeAgo = GetTimeAgo(createdDate)
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