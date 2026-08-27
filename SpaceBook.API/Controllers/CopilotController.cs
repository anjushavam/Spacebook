using System.Security.Claims;
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

    // =========================================================
    // GET CURRENT USER IDENTITY
    // =========================================================
    //
    // Prompt:
    // Who am I? / What is my profile?
    //
    // Supports:
    // 1. Authorization: Bearer <jwt> (JWT claims: sub/nameid, email, name)
    // 2. Query param: ?email=amirtha@valuemomentum.com or ?employeeId=5
    // 3. Header: X-User-Email or X-Employee-Id
    //
    // Examples:
    // GET /api/copilot/me
    // GET /api/copilot/me?email=amirtha.govindasamy@valuemomentum.com
    // GET /api/copilot/user?email=amirtha.govindasamy@valuemomentum.com
    // =========================================================

    [HttpGet("me")]
    [HttpGet("user")]
    public async Task<IActionResult> GetCurrentUser(
        [FromQuery] string? email,
        [FromQuery] int? employeeId,
        [FromHeader(Name = "X-User-Email")] string? headerEmail,
        [FromHeader(Name = "X-Employee-Id")] int? headerEmployeeId)
    {
        try
        {
            // 1. Try JWT Claims
            var claimEmpIdStr =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("employeeId")?.Value
                ?? User.FindFirst("sub")?.Value;

            var claimEmail =
                User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value;

            int? targetEmpId = employeeId ?? headerEmployeeId;
            if (!targetEmpId.HasValue && !string.IsNullOrWhiteSpace(claimEmpIdStr) && int.TryParse(claimEmpIdStr, out int parsedId))
            {
                targetEmpId = parsedId;
            }

            var targetEmail = !string.IsNullOrWhiteSpace(email)
                ? email
                : (!string.IsNullOrWhiteSpace(headerEmail) ? headerEmail : claimEmail);

            if (!targetEmpId.HasValue && string.IsNullOrWhiteSpace(targetEmail))
            {
                return BadRequest(new
                {
                    message = "User identity could not be determined. Please provide a Bearer token, ?email=, ?employeeId=, or X-User-Email header."
                });
            }

            var userProfile =
                await _copilotService.GetUserProfileAsync(targetEmpId, targetEmail);

            if (userProfile == null)
            {
                return NotFound(new
                {
                    message = "Employee not found in SpaceBook."
                });
            }

            return Ok(userProfile);
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
    // GET USER'S ACTIVE & UPCOMING BOOKINGS
    // =========================================================
    //
    // Prompt:
    // What are my bookings? / What reservations do I have today?
    //
    // Supports:
    // 1. Authorization: Bearer <jwt>
    // 2. Query param: ?email=amirtha@valuemomentum.com or ?employeeId=5
    // 3. Header: X-User-Email
    //
    // Examples:
    // GET /api/copilot/my-bookings
    // GET /api/copilot/my-bookings?email=amirtha.govindasamy@valuemomentum.com
    // GET /api/copilot/me/bookings
    // =========================================================

    [HttpGet("my-bookings")]
    [HttpGet("me/bookings")]
    public async Task<IActionResult> GetUserBookings(
        [FromQuery] string? email,
        [FromQuery] int? employeeId,
        [FromQuery] DateOnly? date,
        [FromHeader(Name = "X-User-Email")] string? headerEmail,
        [FromHeader(Name = "X-Employee-Id")] int? headerEmployeeId)
    {
        try
        {
            var claimEmpIdStr =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("employeeId")?.Value
                ?? User.FindFirst("sub")?.Value;

            var claimEmail =
                User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value;

            int? targetEmpId = employeeId ?? headerEmployeeId;
            if (!targetEmpId.HasValue && !string.IsNullOrWhiteSpace(claimEmpIdStr) && int.TryParse(claimEmpIdStr, out int parsedId))
            {
                targetEmpId = parsedId;
            }

            var targetEmail = !string.IsNullOrWhiteSpace(email)
                ? email
                : (!string.IsNullOrWhiteSpace(headerEmail) ? headerEmail : claimEmail);

            if (!targetEmpId.HasValue && string.IsNullOrWhiteSpace(targetEmail))
            {
                return BadRequest(new
                {
                    message = "User identity could not be determined. Please provide a Bearer token, ?email=, ?employeeId=, or X-User-Email header."
                });
            }

            var result =
                await _copilotService.GetUserBookingsAsync(targetEmpId, targetEmail, date);

            if (result == null)
            {
                return NotFound(new
                {
                    message = "Employee not found in SpaceBook."
                });
            }

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

    // =========================================================
    // SEARCH EMPLOYEES
    // =========================================================
    //
    // Prompt:
    // Find employee Amirtha / Search colleague by email or name
    //
    // Examples:
    // GET /api/copilot/employees?search=Amirtha
    // GET /api/copilot/employees?search=valuemomentum.com
    // =========================================================

    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] string? search)
    {
        try
        {
            var result =
                await _copilotService.GetEmployeesAsync(search);

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