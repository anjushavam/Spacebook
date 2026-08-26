using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceBook.Application.DTOs.Hotseat;
using SpaceBook.Application.Interfaces;

namespace SpaceBook.API.Controllers;

[ApiController]
[Route("api/admin/hotseats")]
[Authorize(Roles = "Admin")]
public class AdminHotseatController : ControllerBase
{
    private readonly IAdminHotseatService _service;

    public AdminHotseatController(IAdminHotseatService service)
    {
        _service = service;
    }

    // =========================================================
    // GET: api/admin/hotseats/analytics
    // GET: api/admin/hotseats/dashboard
    // =========================================================

    [HttpGet("analytics")]
    [HttpGet("dashboard")]
    public async Task<ActionResult<HotseatManagementDashboardDto>> GetDashboardAnalytics(
        [FromQuery] string? timeframe,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] string? module,
        [FromQuery] string? status,
        [FromQuery] string? section,
        [FromQuery] string? trendPeriod)
    {
        var filter = new HotseatFilterDto
        {
            Timeframe = timeframe ?? "All Time",
            StartDate = startDate,
            EndDate = endDate,
            Module = module ?? "All Modules",
            Status = status ?? "All Status",
            Section = section,
            TrendPeriod = trendPeriod ?? "Daily"
        };

        var result = await _service.GetHotseatDashboardAnalyticsAsync(filter);
        return Ok(result);
    }

    // =========================================================
    // POST: api/admin/hotseats/analytics
    // =========================================================

    [HttpPost("analytics")]
    public async Task<ActionResult<HotseatManagementDashboardDto>> GetDashboardAnalyticsPost(
        [FromBody] HotseatFilterDto filter)
    {
        var result = await _service.GetHotseatDashboardAnalyticsAsync(filter ?? new HotseatFilterDto());
        return Ok(result);
    }

    // =========================================================
    // GET: api/admin/hotseats/records
    // =========================================================

    [HttpGet("records")]
    public async Task<ActionResult<HotseatAuditPagedResultDto>> GetAuditRecords(
        [FromQuery] string? timeframe,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] string? module,
        [FromQuery] string? status,
        [FromQuery] string? section,
        [FromQuery] string? searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var filter = new HotseatFilterDto
        {
            Timeframe = timeframe ?? "All Time",
            StartDate = startDate,
            EndDate = endDate,
            Module = module ?? "All Modules",
            Status = status ?? "All Status",
            Section = section,
            SearchTerm = searchTerm,
            Page = page > 0 ? page : 1,
            PageSize = pageSize > 0 ? pageSize : 20
        };

        var result = await _service.GetHotseatAuditRecordsAsync(filter);
        return Ok(result);
    }

    // =========================================================
    // POST: api/admin/hotseats/records
    // =========================================================

    [HttpPost("records")]
    public async Task<ActionResult<HotseatAuditPagedResultDto>> GetAuditRecordsPost(
        [FromBody] HotseatFilterDto filter)
    {
        var result = await _service.GetHotseatAuditRecordsAsync(filter ?? new HotseatFilterDto());
        return Ok(result);
    }

    // =========================================================
    // GET: api/admin/hotseats/export-csv
    // =========================================================

    [HttpGet("export-csv")]
    public async Task<IActionResult> ExportCsvGet(
        [FromQuery] string? timeframe,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] string? module,
        [FromQuery] string? status,
        [FromQuery] string? section)
    {
        var filter = new HotseatFilterDto
        {
            Timeframe = timeframe ?? "All Time",
            StartDate = startDate,
            EndDate = endDate,
            Module = module ?? "All Modules",
            Status = status ?? "All Status",
            Section = section
        };

        var csvBytes = await _service.ExportHotseatsCsvAsync(filter);
        var fileName = $"SpaceBook_Hotseat_Report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return File(csvBytes, "text/csv", fileName);
    }

    // =========================================================
    // POST: api/admin/hotseats/export-csv
    // =========================================================

    [HttpPost("export-csv")]
    public async Task<IActionResult> ExportCsvPost(
        [FromBody] HotseatFilterDto filter)
    {
        var csvBytes = await _service.ExportHotseatsCsvAsync(filter ?? new HotseatFilterDto());
        var fileName = $"SpaceBook_Hotseat_Report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return File(csvBytes, "text/csv", fileName);
    }

    // =========================================================
    // GET: api/admin/hotseats/filters
    // =========================================================

    [HttpGet("filters")]
    public async Task<ActionResult<HotseatFilterOptionsDto>> GetFilterOptions()
    {
        var options = await _service.GetFilterOptionsAsync();
        return Ok(options);
    }
}
