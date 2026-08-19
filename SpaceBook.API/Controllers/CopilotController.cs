using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceBook.Application.Interfaces;

namespace SpaceBook.API.Controllers;

[ApiController]
[Route("api/copilot")]
[Authorize(Roles = "Employee")]
public class CopilotController : ControllerBase
{
    private readonly ICopilotService _copilotService;

    public CopilotController(ICopilotService copilotService)
    {
        _copilotService = copilotService;
    }

    // =====================================================
    // GET OFFICES
    // =====================================================

    [HttpGet("offices")]
    public async Task<IActionResult> GetOffices()
    {
        try
        {
            var result = await _copilotService.GetOfficesAsync();

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
    // GET / SEARCH ROOMS
    // =====================================================

    [HttpGet("rooms")]
    public async Task<IActionResult> GetRooms(
        [FromQuery] string? search,
        [FromQuery] int? officeId,
        [FromQuery] int? roomTypeId,
        [FromQuery] int? minCapacity,
        [FromQuery] string? facility)
    {
        try
        {
            var result = await _copilotService.GetRoomsAsync(
                search,
                officeId,
                roomTypeId,
                minCapacity,
                facility);

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
    // GET ROOM AVAILABILITY
    // =====================================================

    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailability(
        [FromQuery] DateOnly date,
        [FromQuery] int? roomTypeId)
    {
        try
        {
            var result = await _copilotService.GetAvailabilityAsync(
                date,
                roomTypeId);

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