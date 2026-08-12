using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceBook.Application.DTOs.Booking;
using SpaceBook.Application.Interfaces;


namespace SpaceBook.API.Controllers;

[ApiController]
[Route("api/employee")]
[Authorize(Roles = "Employee")]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeDashboardService _dashboardService;
    private readonly IEmployeeBookingService _employeeBookingService;

    private readonly IEmployeeCheckInService _checkInService;

    public EmployeeController(
    IEmployeeDashboardService dashboardService,
    IEmployeeBookingService employeeBookingService,
    IEmployeeCheckInService checkInService)
{
    _dashboardService = dashboardService;
    _employeeBookingService = employeeBookingService;
    _checkInService = checkInService;
}

    // Employee Dashboard
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (employeeIdClaim == null)
            {
                return Unauthorized(new
                {
                    Message = "Invalid token. Employee Id not found."
                });
            }

            int employeeId = int.Parse(employeeIdClaim.Value);

            var result = await _dashboardService.GetDashboardAsync(employeeId);

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

    // Employee Availability Calendar
    [HttpGet("availability")]
public async Task<IActionResult> GetAvailability(
    [FromQuery] DateOnly date,
    [FromQuery] int? roomTypeId)
{
    try
    {
        var result = await _dashboardService.GetAvailabilityAsync(date, roomTypeId);
 
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

    // Create Booking
[HttpPost("bookings")]
public async Task<IActionResult> CreateBooking(
    [FromBody] CreateBookingRequestDto request)
{
    try
    {
        var employeeIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier);


        if (employeeIdClaim == null)
        {
            return Unauthorized(new
            {
                Message = "Invalid token."
            });
        }


        int employeeId =
            int.Parse(employeeIdClaim.Value);



        var bookingId =
            await _employeeBookingService
            .CreateBookingAsync(
                employeeId,
                request);



        return Ok(new
        {
            Message = "Booking created successfully.",

            BookingId = bookingId,

            Status = "Pending"
        });
    }
    catch (Exception ex)
    {
        return BadRequest(new
        {
            Message = ex.Message
        });
    }
}

    // My Bookings
    [HttpGet("mybookings")]
    public async Task<IActionResult> GetMyBookings()
    {
        try
        {
            var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (employeeIdClaim == null)
            {
                return Unauthorized(new
                {
                    Message = "Invalid token."
                });
            }

            int employeeId = int.Parse(employeeIdClaim.Value);

            var result = await _dashboardService.GetMyBookingsAsync(employeeId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Message = ex.Message
            });
        }
    }

    // Recent Reservations
    [HttpGet("recentreservations")]
    public async Task<IActionResult> GetRecentReservations()
    {
        try
        {
            var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (employeeIdClaim == null)
            {
                return Unauthorized(new
                {
                    Message = "Invalid token."
                });
            }

            int employeeId = int.Parse(employeeIdClaim.Value);

            var result = await _dashboardService.GetRecentReservationsAsync(employeeId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Message = ex.Message
            });
        }
    }
    // View Booking
[HttpGet("bookings/{bookingId}")]
public async Task<IActionResult> GetBookingById(int bookingId)
{
    try
    {
        var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (employeeIdClaim == null)
        {
            return Unauthorized(new
            {
                Message = "Invalid token."
            });
        }

        int employeeId = int.Parse(employeeIdClaim.Value);

        var booking = await _employeeBookingService.GetBookingByIdAsync(
            bookingId,
            employeeId);

        if (booking == null)
        {
            return NotFound(new
            {
                Message = "Booking not found."
            });
        }

        return Ok(booking);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            Message = ex.Message
        });
    }
}
// Cancel Booking
[HttpPut("bookings/{bookingId}/cancel")]
public async Task<IActionResult> CancelBooking(int bookingId)
{
    try
    {
        var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (employeeIdClaim == null)
        {
            return Unauthorized(new
            {
                Message = "Invalid token."
            });
        }

        int employeeId = int.Parse(employeeIdClaim.Value);

        var result = await _employeeBookingService.CancelBookingAsync(
            bookingId,
            employeeId);

        if (!result)
        {
            return NotFound(new
            {
                Message = "Booking not found."
            });
        }

        return Ok(new
        {
            Message = "Booking cancelled successfully."
        });
    }
    catch (Exception ex)
    {
        return BadRequest(new
        {
            Message = ex.Message
        });
    }
}
// Edit Booking
[HttpPut("bookings/{bookingId}")]
public async Task<IActionResult> UpdateBooking(
    int bookingId,
    [FromBody] UpdateBookingRequestDto request)
{
    try
    {
        var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (employeeIdClaim == null)
        {
            return Unauthorized(new
            {
                Message = "Invalid token."
            });
        }

        int employeeId = int.Parse(employeeIdClaim.Value);

        var result = await _employeeBookingService.UpdateBookingAsync(
            bookingId,
            employeeId,
            request);

        if (!result)
        {
            return NotFound(new
            {
                Message = "Booking not found."
            });
        }

        return Ok(new
        {
            Message = "Booking updated successfully."
        });
    }
    catch (Exception ex)
    {
        return BadRequest(new
        {
            Message = ex.Message
        });
    }
}

// Get Rooms By Module
[HttpGet("rooms")]
public async Task<IActionResult> GetRoomsByModule(
    [FromQuery] string module)
{
    try
    {
        if (string.IsNullOrWhiteSpace(module))
        {
            return BadRequest(new
            {
                Message = "Module is required."
            });
        }

        var rooms = await _employeeBookingService
            .GetRoomsByModuleAsync(module);

        return Ok(rooms);
    }
    catch (Exception ex)
    {
        return BadRequest(new
        {
            Message = ex.Message
        });
    }
}

// Search Available Rooms
[HttpPost("searchrooms")]
public async Task<IActionResult> SearchAvailableRooms(
    [FromBody] SearchRoomsRequestDto request)
{
    try
    {
        var rooms = await _employeeBookingService.SearchAvailableRoomsAsync(request);

        return Ok(rooms);
    }
    catch (Exception ex)
    {
        return BadRequest(new
        {
            Message = ex.Message
        });
    }
}

// Check-In Booking
[HttpPost("bookings/{bookingId}/checkin")]
public async Task<IActionResult> CheckIn(
    int bookingId)
{
    try
    {
        var employeeIdClaim =
            User.FindFirst(
                ClaimTypes.NameIdentifier);


        if(employeeIdClaim == null)
        {
            return Unauthorized(new
            {
                Message = "Invalid token."
            });
        }


        int employeeId =
            int.Parse(employeeIdClaim.Value);



        var result =
            await _checkInService
            .CheckInAsync(
                bookingId,
                employeeId);



        return Ok(result);
    }
    catch(Exception ex)
    {
        return BadRequest(new
        {
            Message = ex.Message
        });
    }
}

}