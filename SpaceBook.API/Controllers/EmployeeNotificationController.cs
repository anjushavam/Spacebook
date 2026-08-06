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

            var employeeIdClaim =

                User.FindFirst(ClaimTypes.NameIdentifier);
 
 
            if(employeeIdClaim == null)

            {

                return Unauthorized(new

                {

                    message = "Invalid token."

                });

            }
 
 
            int employeeId =

                int.Parse(employeeIdClaim.Value);
 
 
            var notifications =

                await _notificationRepository

                .GetEmployeeNotificationsAsync(employeeId);
 
 
            return Ok(notifications);

        }

        catch(Exception ex)

        {

            return StatusCode(500,new

            {

                message = "Something went wrong.",

                error = ex.Message

            });

        }

    }

}
 