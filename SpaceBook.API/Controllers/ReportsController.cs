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
}