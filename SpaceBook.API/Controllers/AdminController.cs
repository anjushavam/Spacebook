using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceBook.Application.Interfaces;

using SpaceBook.Application.DTOs.Admin;

namespace SpaceBook.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _service;

    public AdminController(IAdminService service)
    {
        _service = service;
    }

    // =========================================================
    // GET: api/admin/dashboard
    // =========================================================

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(
        [FromQuery] string? timeframe,
        [FromQuery] string? module,
        [FromQuery] string? status,
        [FromQuery] int? month,
        [FromQuery] int? year,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] int? roomTypeId)
    {
        var filter = new AdminDashboardFilterDto
        {
            Timeframe = timeframe,
            Module = module,
            Status = status,
            Month = month,
            Year = year,
            StartDate = startDate,
            EndDate = endDate,
            RoomTypeId = roomTypeId
        };

        var data = await _service.GetDashboardAsync(filter);

        return Ok(data);
    }

    // =========================================================
    // POST: api/admin/dashboard
    // =========================================================

    [HttpPost("dashboard")]
    public async Task<IActionResult> DashboardPost(
        [FromBody] AdminDashboardFilterDto? filter)
    {
        var data = await _service.GetDashboardAsync(filter);

        return Ok(data);
    }
}