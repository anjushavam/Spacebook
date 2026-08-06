using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
 
 
    // GET: api/admin/notifications
    // Admin views missed check-in notifications
    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        try
        {
            var notifications =
                await _notificationRepository.GetAllAsync();
 
            return Ok(notifications);
        }
        catch(Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Something went wrong.",
                error = ex.Message
            });
        }
    }
}