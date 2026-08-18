using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceBook.Application.Interfaces;
using System.Security.Claims;

namespace SpaceBook.API.Controllers;

[ApiController]
[Route("api/copilot")]
[Authorize(Roles = "Employee")]
public class CopilotController : ControllerBase
{
    private readonly IEmployeeDashboardService _dashboardService;

    public CopilotController(
        IEmployeeDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    // =====================================================
    // GET AVAILABILITY
    // =====================================================

    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailability(
        [FromQuery] DateOnly? date,
        [FromQuery] int? roomTypeId)
    {
        try
        {
            if (!date.HasValue)
            {
                return BadRequest(new
                {
                    message = "Date is required."
                });
            }

            var result = await _dashboardService
                .GetAvailabilityAsync(date.Value, roomTypeId);

            return Ok(result);
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

    // =====================================================
    // GET MY BOOKINGS
    // =====================================================

    [HttpGet("my-bookings")]
    public async Task<IActionResult> GetMyBookings()
    {
        try
        {
            // EmployeeId comes from the authenticated JWT
            var employeeIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(employeeIdClaim))
            {
                return Unauthorized(new
                {
                    message = "Employee information not found in token."
                });
            }

            if (!int.TryParse(employeeIdClaim, out var employeeId))
            {
                return Unauthorized(new
                {
                    message = "Invalid employee information in token."
                });
            }

            var result = await _dashboardService
                .GetMyBookingsAsync(employeeId);

            return Ok(result);
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