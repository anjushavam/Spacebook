using Microsoft.AspNetCore.Mvc;
using SpaceBook.Application.Interfaces;

namespace SpaceBook.API.Controllers;

[ApiController]
[Route("api/copilot")]
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
        [FromQuery] DateOnly date,
        [FromQuery] int? roomTypeId)
    {
        try
        {
            var result = await _dashboardService
                .GetAvailabilityAsync(date, roomTypeId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Message = "Something went wrong.",
                Error = ex.Message
            });
        }
    }
}