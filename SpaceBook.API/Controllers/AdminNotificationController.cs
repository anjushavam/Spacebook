using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceBook.Application.DTOs.Admin;
using SpaceBook.Application.Interfaces;

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
            // Fetch raw entity records from database repository first
            var notifications = await _notificationRepository.GetAllAsync();

            // Perform mapping in memory to prevent EF Core expression tree translation errors
            var response = notifications.Select(n =>
            {
                var message = n.Message ?? string.Empty;

                // Dynamically derive a clean title based on message content
                string title = "Missed Check-in";
                if (message.Contains("approved", StringComparison.OrdinalIgnoreCase) ||
                    message.Contains("confirmed", StringComparison.OrdinalIgnoreCase))
                {
                    title = "Booking Approved";
                }
                else if (message.Contains("rejected", StringComparison.OrdinalIgnoreCase) ||
                         message.Contains("cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    title = "Booking Cancelled";
                }
                else if (message.Contains("requested", StringComparison.OrdinalIgnoreCase) ||
                         message.Contains("pending", StringComparison.OrdinalIgnoreCase))
                {
                    title = "Booking Request";
                }

                // Ensure n.CreatedAt is not default (0001-01-01)
                var createdDate = n.CreatedAt == default ? DateTime.UtcNow : n.CreatedAt;

                // Map DB Entity (n.CreatedAt) to NotificationDto (CreatedOn)
                return new NotificationDto
                {
                    NotificationId = n.NotificationId,
                    Title = title,
                    Message = message,
                    IsRead = n.IsRead,
                    CreatedOn = createdDate,
                    TimeAgo = GetTimeAgo(createdDate)
                };
            }).ToList();

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