using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceBook.Application.DTOs.Reports;
using SpaceBook.Application.Interfaces;

namespace SpaceBook.API.Controllers;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _service;

    public ReportsController(IReportService service)
    {
        _service = service;
    }

    [HttpPost("bookingtrend")]
    public async Task<IActionResult> BookingTrend(
        ReportFilterDto filter)
    {
        return Ok(await _service.GetBookingTrendAsync(filter));
    }

    [HttpPost("bookingstatus")]
    public async Task<IActionResult> BookingStatus(
        ReportFilterDto filter)
    {
        return Ok(await _service.GetBookingStatusAsync(filter));
    }

    [HttpPost("roomusage")]
    public async Task<IActionResult> RoomUsage(
        ReportFilterDto filter)
    {
        return Ok(await _service.GetRoomUsageAsync(filter));
    }

    [HttpPost("analytics")]
    public async Task<IActionResult> GetAnalyticsPost(
        [FromBody] ReportFilterDto filter)
    {
        return Ok(await _service.GetWorkplaceAnalyticsAsync(filter));
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalyticsGet(
        [FromQuery] string? timeframe,
        [FromQuery] string? module,
        [FromQuery] int? roomTypeId,
        [FromQuery] string? status,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate)
    {
        var filter = new ReportFilterDto
        {
            Timeframe = timeframe,
            Module = module,
            RoomTypeId = roomTypeId,
            Status = status,
            StartDate = startDate,
            EndDate = endDate
        };

        return Ok(await _service.GetWorkplaceAnalyticsAsync(filter));
    }

    [HttpPost("export-csv")]
    public async Task<IActionResult> ExportCsvPost(
        [FromBody] ReportFilterDto filter)
    {
        var csvBytes = await _service.ExportBookingsCsvAsync(filter);
        var fileName = $"SpaceBook_Bookings_Report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return File(csvBytes, "text/csv", fileName);
    }

    [HttpGet("export-csv")]
    public async Task<IActionResult> ExportCsvGet(
        [FromQuery] string? reportType,
        [FromQuery] string? module,
        [FromQuery] int? roomTypeId,
        [FromQuery] string? status)
    {
        var filter = new ReportFilterDto
        {
            ReportType = reportType,
            Module = module,
            RoomTypeId = roomTypeId,
            Status = status
        };

        var csvBytes = await _service.ExportBookingsCsvAsync(filter);
        var fileName = $"SpaceBook_Bookings_Report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return File(csvBytes, "text/csv", fileName);
    }
}