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

    // =========================================================
    // GET DASHBOARD
    // =========================================================

    // GET: api/admin/bookings/dashboard
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var result =
            await _bookingService.GetDashboardAsync();

        return Ok(result);
    }

    // =========================================================
    // GET ALL BOOKINGS
    // =========================================================

    // GET: api/admin/bookings
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] BookingFilterDto filter)
    {
        var bookings =
            await _bookingService.GetAllAsync(filter);

        return Ok(bookings);
    }

    // =========================================================
    // GET BOOKING BY ID
    // =========================================================

    // GET: api/admin/bookings/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var booking =
            await _bookingService.GetByIdAsync(id);

        if (booking == null)
        {
            return NotFound(new
            {
                message = "Booking not found."
            });
        }

        return Ok(booking);
    }

    // =========================================================
    // DELETE BOOKING
    // =========================================================

    // DELETE: api/admin/bookings/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _bookingService.DeleteAsync(id);

            return Ok(new
            {
                message = "Booking deleted successfully."
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new
            {
                message = "Booking not found."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "An unexpected error occurred while deleting the booking.",
                    error = ex.Message
                });
        }
    }
}