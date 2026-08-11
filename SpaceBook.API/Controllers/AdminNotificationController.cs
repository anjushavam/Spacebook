using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceBook.Application.DTOs.Employee;
using SpaceBook.Application.Interfaces;

namespace SpaceBook.API.Controllers;

[ApiController]
[Route("api/admin/notifications")]
[Authorize(Roles = "Admin")]
public class AdminNotificationController : ControllerBase
{
    private readonly INotificationRepository _notificationRepository;

    public AdminNotificationController(
        INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }


    // =========================================================
    // GET: api/admin/notifications
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        try
        {
            var notifications =
                await _notificationRepository.GetAdminNotificationsAsync();


            var response = notifications
                .Select(n =>
                {
                    var message =
                        n.Message ?? string.Empty;


                    var title =
                        string.IsNullOrEmpty(n.Title)
                            ? "Booking Request"
                            : n.Title;


                    var createdDate =
                        n.CreatedOn == default
                            ? DateTime.UtcNow
                            : n.CreatedOn;


                    return new NotificationDto
                    {
                        NotificationId =
                            n.NotificationId,

                        Title =
                            title,

                        Message =
                            message,

                        IsRead =
                            n.IsRead,

                        CreatedOn =
                            createdDate,

                        TimeAgo =
                            string.IsNullOrEmpty(n.TimeAgo)
                                ? GetTimeAgo(createdDate)
                                : n.TimeAgo,


                        // =================================================
                        // Booking Information
                        // =================================================

                        EmployeeName =
                            n.EmployeeName,

                        RoomName =
                            n.RoomName,

                        BookingDate =
                            n.BookingDate,

                        StartTime =
                            n.StartTime,

                        EndTime =
                            n.EndTime
                    };
                })
                .OrderByDescending(
                    x => x.NotificationId)
                .ToList();


            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new
                {
                    message = "Something went wrong.",
                    error = ex.Message
                });
        }
    }


    // =========================================================
    // PATCH: api/admin/notifications/read-all
    // =========================================================

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            // Admin uses employeeId = 0.
            // Repository should interpret this as all admin
            // notifications.

            await _notificationRepository
                .MarkAllAsReadAsync(0);


            return Ok(
                new
                {
                    message =
                        "All admin notifications marked as read."
                });
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new
                {
                    message =
                        "Something went wrong.",

                    error =
                        ex.Message
                });
        }
    }


    // =========================================================
    // Time Ago
    // =========================================================

    private static string GetTimeAgo(
        DateTime dateTime)
    {
        var utcDate =
            dateTime.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(
                    dateTime,
                    DateTimeKind.Utc)
                : dateTime.ToUniversalTime();


        var timeSpan =
            DateTime.UtcNow - utcDate;


        if (
            timeSpan.TotalSeconds < 0 ||
            timeSpan.TotalSeconds < 60)
        {
            return "Just now";
        }


        if (timeSpan.TotalMinutes < 60)
        {
            return
                $"{(int)timeSpan.TotalMinutes}m ago";
        }


        if (timeSpan.TotalHours < 24)
        {
            return
                $"{(int)timeSpan.TotalHours}h ago";
        }


        if (timeSpan.TotalDays < 7)
        {
            return
                $"{(int)timeSpan.TotalDays}d ago";
        }


        return dateTime.ToString(
            "MMM dd, yyyy");
    }
}