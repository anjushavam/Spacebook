using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceBook.Application.Interfaces;

namespace SpaceBook.API.Controllers;

[ApiController]
[Route("api/employee/notifications")]
[Authorize(Roles = "Employee")]
public class EmployeeNotificationController : ControllerBase
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationService _notificationService;

    public EmployeeNotificationController(
        INotificationRepository notificationRepository,
        INotificationService notificationService)
    {
        _notificationRepository = notificationRepository;
        _notificationService = notificationService;
    }

    // =========================================================
    // GET: api/employee/notifications
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        try
        {
            var employeeIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (employeeIdClaim == null ||
                !int.TryParse(
                    employeeIdClaim.Value,
                    out int employeeId))
            {
                return Unauthorized(new
                {
                    message = "Invalid token."
                });
            }

            var notifications =
                await _notificationRepository
                    .GetEmployeeNotificationsAsync(employeeId);

            return Ok(notifications);
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

    // =========================================================
    // PATCH: api/employee/notifications/read-all
    // =========================================================

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var employeeIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (employeeIdClaim == null ||
                !int.TryParse(
                    employeeIdClaim.Value,
                    out int employeeId))
            {
                return Unauthorized(new
                {
                    message = "Invalid token."
                });
            }

            await _notificationService
                .MarkAllAsReadAsync(employeeId);

            return Ok(new
            {
                message =
                    "All notifications marked as read."
            });
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

    // =========================================================
    // DELETE: api/employee/notifications
    // DELETE: api/employee/notifications/clear-all
    // =========================================================

    [HttpDelete]
    [HttpDelete("clear-all")]
    public async Task<IActionResult> ClearAllNotifications()
    {
        try
        {
            var employeeIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (employeeIdClaim == null ||
                !int.TryParse(
                    employeeIdClaim.Value,
                    out int employeeId))
            {
                return Unauthorized(new
                {
                    message = "Invalid token."
                });
            }

            await _notificationService
                .ClearNotificationsForUserAsync(employeeId);

            return Ok(new
            {
                message = "All notifications cleared successfully."
            });
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

    // =========================================================
    // DELETE: api/employee/notifications/{id}
    // =========================================================

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        try
        {
            var employeeIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (employeeIdClaim == null ||
                !int.TryParse(
                    employeeIdClaim.Value,
                    out int employeeId))
            {
                return Unauthorized(new
                {
                    message = "Invalid token."
                });
            }

            await _notificationService
                .DeleteNotificationAsync(id, employeeId);

            return Ok(new
            {
                message = "Notification deleted successfully."
            });
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
}