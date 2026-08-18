using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceBook.Application.Interfaces;

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

    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailability(
        [FromQuery] DateOnly? date,
        [FromQuery] int? roomTypeId)
    {
        try
        {
            // Date is required
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
}