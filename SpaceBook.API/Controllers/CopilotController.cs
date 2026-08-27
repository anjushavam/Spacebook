using Microsoft.AspNetCore.Mvc;
using SpaceBook.Application.DTOs.Copilot;
using SpaceBook.Application.Interfaces;

namespace SpaceBook.API.Controllers;

[ApiController]
[Route("api/copilot")]
public class CopilotController : ControllerBase
{
    private readonly ICopilotService _copilotService;

    public CopilotController(ICopilotService copilotService)
    {
        _copilotService = copilotService;
    }

    // =========================================================
    // GET OFFICES
    // =========================================================
    //
    // Prompt 1:
    // What office locations are currently available?
    //
    // Prompt 2:
    // Which office is located in Coimbatore?
    //
    // Examples:
    //
    // GET /api/copilot/offices
    //
    // GET /api/copilot/offices?search=Coimbatore
    //
    // GET /api/copilot/offices?search=Elcot
    // =========================================================

    [HttpGet("offices")]
    public async Task<IActionResult> GetOffices(
        [FromQuery] string? search)
    {
        try
        {
            var result =
                await _copilotService.GetOfficesAsync(search);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
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
    // GET / SEARCH ROOMS
    // =========================================================
    //
    // Prompt 3:
    // What rooms are currently available in the Coimbatore office?
    //
    // Prompt 4:
    // Can you find Conference Room in the Coimbatore office?
    //
    // Prompt 5:
    // Which rooms in the Coimbatore office have a capacity
    // of at least 10 people?
    //
    // Prompt 6:
    // Can you provide the details of Conference Room
    // in the Coimbatore office?
    //
    // Examples:
    //
    // GET /api/copilot/rooms?search=Coimbatore
    //
    // GET /api/copilot/rooms?search=Conference%20Room
    //
    // GET /api/copilot/rooms?officeId=1
    //
    // GET /api/copilot/rooms?officeId=1&minCapacity=10
    //
    // GET /api/copilot/rooms?search=Conference%20Room&officeId=1
    // =========================================================

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
            var result =
                await _copilotService.GetRoomsAsync(
                    search,
                    officeId,
                    roomTypeId,
                    minCapacity,
                    facility);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
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
    // GET ROOM AVAILABILITY
    // =========================================================
    //
    // Example:
    //
    // GET /api/copilot/availability?date=2026-08-19
    //
    // GET /api/copilot/availability
    // is NOT recommended because date is required.
    // =========================================================

    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailability(
        [FromQuery] DateOnly date,
        [FromQuery] int? roomTypeId)
    {
        try
        {
            var result =
                await _copilotService.GetAvailabilityAsync(
                    date,
                    roomTypeId);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
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
    // GET ROOM RECOMMENDATIONS
    // =========================================================
    //
    // Example:
    //
    // POST /api/copilot/recommendations
    //
    // {
    //   "date": "2026-08-19",
    //   "startTime": "14:00:00",
    //   "endTime": "15:00:00",
    //   "participantCount": 5,
    //   "officeId": 1,
    //   "roomTypeId": null,
    //   "facility": null
    // }
    // =========================================================

    [HttpPost("recommendations")]
    public async Task<IActionResult> GetRecommendations(
        [FromBody] CopilotRecommendationRequestDto request)
    {
        try
        {
            var result =
                await _copilotService.GetRecommendationsAsync(request);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
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
    // GET HOTSEAT SUMMARY & AVAILABILITY
    // =========================================================
    //
    // Prompt:
    // How many hotseats are available today in Coimbatore?
    // What is the hotseat summary (available, booked, cancelled)?
    // In what locations are hotseats available?
    //
    // Examples:
    // GET /api/copilot/hotseats/summary
    // GET /api/copilot/hotseats/summary?date=2026-08-27&location=Coimbatore
    // GET /api/copilot/hotseats/summary?module=Module%201
    // =========================================================

    [HttpGet("hotseats/summary")]
    [HttpGet("hotseats/availability")]
    public async Task<IActionResult> GetHotseatSummary(
        [FromQuery] DateOnly? date,
        [FromQuery] string? location,
        [FromQuery] string? office,
        [FromQuery] string? module)
    {
        try
        {
            var result =
                await _copilotService.GetHotseatSummaryAsync(
                    date,
                    location,
                    office,
                    module);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
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
    // GET / SEARCH HOTSEATS
    // =========================================================
    //
    // Examples:
    // GET /api/copilot/hotseats
    // GET /api/copilot/hotseats?search=WS-05
    // GET /api/copilot/hotseats?location=Coimbatore&status=Available
    // GET /api/copilot/hotseats?module=Module%201&section=Section%20A
    // =========================================================

    [HttpGet("hotseats")]
    public async Task<IActionResult> GetHotseats(
        [FromQuery] string? search,
        [FromQuery] DateOnly? date,
        [FromQuery] string? location,
        [FromQuery] int? officeId,
        [FromQuery] string? office,
        [FromQuery] string? module,
        [FromQuery] string? section,
        [FromQuery] string? status)
    {
        try
        {
            var filter = new HotseatSearchFilterCopilotDto
            {
                Search = search,
                Date = date,
                Location = location,
                OfficeId = officeId,
                Office = office,
                Module = module,
                Section = section,
                Status = status
            };

            var result =
                await _copilotService.GetHotseatsAsync(filter);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
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
    // GET HOTSEAT LOCATIONS
    // =========================================================
    //
    // Prompt:
    // In what locations are hotseats available?
    //
    // Example:
    // GET /api/copilot/hotseats/locations
    // =========================================================

    [HttpGet("hotseats/locations")]
    public async Task<IActionResult> GetHotseatLocations()
    {
        try
        {
            var result =
                await _copilotService.GetHotseatLocationsAsync();

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
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