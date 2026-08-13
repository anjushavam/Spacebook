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

    // =========================================================
    // EMPLOYEE DASHBOARD
    // =========================================================

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var employeeIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (employeeIdClaim == null)
            {
                return Unauthorized(new
                {
                    Message = "Invalid token. Employee Id not found."
                });
            }

            if (!int.TryParse(
                    employeeIdClaim.Value,
                    out int employeeId))
            {
                return Unauthorized(new
                {
                    Message = "Invalid employee Id in token."
                });
            }

            var result =
                await _dashboardService.GetDashboardAsync(
                    employeeId);

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

    // =========================================================
    // EMPLOYEE AVAILABILITY CALENDAR
    // =========================================================

    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailability(
        [FromQuery] DateOnly? date,
        [FromQuery] int? roomTypeId)
    {
        try
        {
            // -------------------------------------------------
            // DATE REQUIRED
            // -------------------------------------------------

            if (!date.HasValue)
            {
                return BadRequest(new
                {
                    Message = "Date is required."
                });
            }

            DateOnly bookingDate = date.Value;

            // -------------------------------------------------
            // GET TODAY
            // -------------------------------------------------

            DateOnly today =
                DateOnly.FromDateTime(DateTime.Now);

            // -------------------------------------------------
            // PREVENT PAST DATE
            // -------------------------------------------------

            if (bookingDate < today)
            {
                return BadRequest(new
                {
                    Message =
                        $"Cannot check availability for a past date. " +
                        $"Today is {today:yyyy-MM-dd}."
                });
            }

            // -------------------------------------------------
            // PREVENT SATURDAY
            // -------------------------------------------------

            if (bookingDate.DayOfWeek == DayOfWeek.Saturday)
            {
                return BadRequest(new
                {
                    Message =
                        "Room availability is not available on Saturdays."
                });
            }

            // -------------------------------------------------
            // PREVENT SUNDAY
            // -------------------------------------------------

            if (bookingDate.DayOfWeek == DayOfWeek.Sunday)
            {
                return BadRequest(new
                {
                    Message =
                        "Room availability is not available on Sundays."
                });
            }

            // -------------------------------------------------
            // VALID ROOM TYPE
            // -------------------------------------------------

            if (roomTypeId.HasValue &&
                roomTypeId.Value <= 0)
            {
                return BadRequest(new
                {
                    Message = "Invalid room type."
                });
            }

            // -------------------------------------------------
            // GET AVAILABILITY
            //
            // IMPORTANT:
            // This will NOT execute for:
            // - Past dates
            // - Saturdays
            // - Sundays
            // -------------------------------------------------

            var result =
                await _dashboardService.GetAvailabilityAsync(
                    bookingDate,
                    roomTypeId);

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

    // =========================================================
    // CREATE BOOKING
    // =========================================================

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

            if (!int.TryParse(
                    employeeIdClaim.Value,
                    out int employeeId))
            {
                return Unauthorized(new
                {
                    Message = "Invalid employee Id in token."
                });
            }

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

    // =========================================================
    // MY BOOKINGS
    // =========================================================

    [HttpGet("mybookings")]
    public async Task<IActionResult> GetMyBookings()
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

            if (!int.TryParse(
                    employeeIdClaim.Value,
                    out int employeeId))
            {
                return Unauthorized(new
                {
                    Message = "Invalid employee Id in token."
                });
            }

            var result =
                await _dashboardService
                    .GetMyBookingsAsync(employeeId);

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

    // =========================================================
    // RECENT RESERVATIONS
    // =========================================================

    [HttpGet("recentreservations")]
    public async Task<IActionResult> GetRecentReservations()
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

            if (!int.TryParse(
                    employeeIdClaim.Value,
                    out int employeeId))
            {
                return Unauthorized(new
                {
                    Message = "Invalid employee Id in token."
                });
            }

            var result =
                await _dashboardService
                    .GetRecentReservationsAsync(employeeId);

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

    // =========================================================
    // VIEW BOOKING
    // =========================================================

    [HttpGet("bookings/{bookingId}")]
    public async Task<IActionResult> GetBookingById(
        int bookingId)
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

            if (!int.TryParse(
                    employeeIdClaim.Value,
                    out int employeeId))
            {
                return Unauthorized(new
                {
                    Message = "Invalid employee Id in token."
                });
            }

            var booking =
                await _employeeBookingService
                    .GetBookingByIdAsync(
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

    // =========================================================
    // CANCEL BOOKING
    // =========================================================

    [HttpPut("bookings/{bookingId}/cancel")]
    public async Task<IActionResult> CancelBooking(
        int bookingId)
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

            if (!int.TryParse(
                    employeeIdClaim.Value,
                    out int employeeId))
            {
                return Unauthorized(new
                {
                    Message = "Invalid employee Id in token."
                });
            }

            var result =
                await _employeeBookingService
                    .CancelBookingAsync(
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

    // =========================================================
    // EDIT / RESCHEDULE BOOKING
    // =========================================================

    [HttpPut("bookings/{bookingId}")]
    public async Task<IActionResult> UpdateBooking(
        int bookingId,
        [FromBody] UpdateBookingRequestDto request)
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

            if (!int.TryParse(
                    employeeIdClaim.Value,
                    out int employeeId))
            {
                return Unauthorized(new
                {
                    Message = "Invalid employee Id in token."
                });
            }

            var result =
                await _employeeBookingService
                    .UpdateBookingAsync(
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

    // =========================================================
    // GET ROOMS BY MODULE
    // =========================================================

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

            var rooms =
                await _employeeBookingService
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

    // =========================================================
    // SEARCH AVAILABLE ROOMS
    // =========================================================

    [HttpPost("searchrooms")]
    public async Task<IActionResult> SearchAvailableRooms(
        [FromBody] SearchRoomsRequestDto request)
    {
        try
        {
            // -------------------------------------------------
            // VALIDATE DATE
            // -------------------------------------------------

            if (request.BookingDate.HasValue)
            {
                DateOnly bookingDate =
                    request.BookingDate.Value;

                DateOnly today =
                    DateOnly.FromDateTime(DateTime.Now);

                if (bookingDate < today)
                {
                    return BadRequest(new
                    {
                        Message =
                            "Cannot search rooms for a past date."
                    });
                }

                if (bookingDate.DayOfWeek ==
                    DayOfWeek.Saturday)
                {
                    return BadRequest(new
                    {
                        Message =
                            "Room search is not available on Saturdays."
                    });
                }

                if (bookingDate.DayOfWeek ==
                    DayOfWeek.Sunday)
                {
                    return BadRequest(new
                    {
                        Message =
                            "Room search is not available on Sundays."
                    });
                }
            }

            // -------------------------------------------------
            // VALIDATE TIME
            // -------------------------------------------------

            if (request.StartTime.HasValue &&
                request.EndTime.HasValue)
            {
                if (request.StartTime.Value >=
                    request.EndTime.Value)
                {
                    return BadRequest(new
                    {
                        Message =
                            "Start time must be earlier than end time."
                    });
                }
            }

            // -------------------------------------------------
            // SEARCH ROOMS
            // -------------------------------------------------

            var rooms =
                await _employeeBookingService
                    .SearchAvailableRoomsAsync(request);

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

    // =========================================================
    // CHECK-IN BOOKING
    // =========================================================

    [HttpPost("bookings/{bookingId}/checkin")]
    public async Task<IActionResult> CheckIn(
        int bookingId)
    {
        try
        {
            var employeeIdClaim =
                User.FindFirst(
                    ClaimTypes.NameIdentifier);

            if (employeeIdClaim == null)
            {
                return Unauthorized(new
                {
                    Message = "Invalid token."
                });
            }

            if (!int.TryParse(
                    employeeIdClaim.Value,
                    out int employeeId))
            {
                return Unauthorized(new
                {
                    Message = "Invalid employee Id in token."
                });
            }

            var result =
                await _checkInService
                    .CheckInAsync(
                        bookingId,
                        employeeId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Message = ex.Message
            });
        }
    }
}