using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceBook.Application.DTOs.Booking;
using SpaceBook.Application.Interfaces;

namespace SpaceBook.API.Controllers;

[ApiController]
[Route("api/admin/bookings")]
[Authorize(Roles = "Admin")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    // GET: api/admin/bookings/dashboard
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await _bookingService.GetDashboardAsync();
        return Ok(result);
    }

    // GET: api/admin/bookings
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] BookingFilterDto filter)
    {
        var bookings = await _bookingService.GetAllAsync(filter);
        return Ok(bookings);
    }

    // GET: api/admin/bookings/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var booking = await _bookingService.GetByIdAsync(id);

        if (booking == null)
            return NotFound(new { message = "Booking not found." });

        return Ok(booking);
    }

    // PATCH: api/admin/bookings/1/approve
    [HttpPatch("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        await _bookingService.ApproveAsync(id);

        return Ok(new
        {
            message = "Booking approved successfully."
        });
    }

    // PATCH: api/admin/bookings/1/reject
    [HttpPatch("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id)
    {
        await _bookingService.RejectAsync(id);

        return Ok(new
        {
            message = "Booking rejected successfully."
        });
    }

    // DELETE: api/admin/bookings/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _bookingService.DeleteAsync(id);

        return Ok(new
        {
            message = "Booking deleted successfully."
        });
    }
}