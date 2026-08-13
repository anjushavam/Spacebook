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
    // GET EMPLOYEE ID FROM TOKEN
    // =========================================================

    private bool TryGetEmployeeId(out int employeeId)
    {
        employeeId = 0;

        var employeeIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier);

        if (employeeIdClaim == null)
        {
            return false;
        }

        return int.TryParse(
            employeeIdClaim.Value,
            out employeeId);
    }

    // =========================================================
    // VALIDATE BOOKING DATE
    // =========================================================

    private IActionResult? ValidateBookingDate(DateOnly bookingDate)
    {
        // -----------------------------------------------------
        // TODAY
        // -----------------------------------------------------

        var today =
            DateOnly.FromDateTime(DateTime.Now);

        // -----------------------------------------------------
        // PAST DATE
        // -----------------------------------------------------

        if (bookingDate < today)
        {
            return BadRequest(new
            {
                Message =
                    $"Cannot check availability for a past date. " +
                    $"Today is {today:yyyy-MM-dd}."
            });
        }

        // -----------------------------------------------------
        // SATURDAY
        // -----------------------------------------------------

        if (bookingDate.DayOfWeek == DayOfWeek.Saturday)
        {
            return BadRequest(new
            {
                Message =
                    "Room availability is not available on Saturdays."
            });
        }

        // -----------------------------------------------------
        // SUNDAY
        // -----------------------------------------------------

        if (bookingDate.DayOfWeek == DayOfWeek.Sunday)
        {
            return BadRequest(new
            {
                Message =
                    "Room availability is not available on Sundays."
            });
        }

        return null;
    }

    // =========================================================
    // EMPLOYEE DASHBOARD
    // =========================================================

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            // -------------------------------------------------
            // GET EMPLOYEE ID
            // -------------------------------------------------

            if (!TryGetEmployeeId(out int employeeId))
            {
                return Unauthorized(new
                {
                    Message =
                        "Invalid token. Employee Id not found."
                });
            }

            // -------------------------------------------------
            // GET DASHBOARD
            // -------------------------------------------------

            var result =
                await _dashboardService
                    .GetDashboardAsync(employeeId);

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

            var bookingDate = date.Value;

            // -------------------------------------------------
            // VALIDATE DATE
            //
            // This rejects:
            // 1. Past dates
            // 2. Saturdays
            // 3. Sundays
            // -------------------------------------------------

            var dateValidation =
                ValidateBookingDate(bookingDate);

            if (dateValidation != null)
            {
                return dateValidation;
            }

            // -------------------------------------------------
            // VALIDATE ROOM TYPE
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
            //
            // This service will ONLY execute after the
            // date validation above has passed.
            // -------------------------------------------------

            var result =
                await _dashboardService
                    .GetAvailabilityAsync(
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
            // -------------------------------------------------
            // GET EMPLOYEE ID
            // -------------------------------------------------

            if (!TryGetEmployeeId(out int employeeId))
            {
                return Unauthorized(new
                {
                    Message =
                        "Invalid token. Employee Id not found."
                });
            }

            // -------------------------------------------------
            // VALIDATE REQUEST
            // -------------------------------------------------

            if (request == null)
            {
                return BadRequest(new
                {
                    Message = "Booking request is required."
                });
            }

            // -------------------------------------------------
            // CREATE BOOKING
            // -------------------------------------------------

            var bookingId =
                await _employeeBookingService
                    .CreateBookingAsync(
                        employeeId,
                        request);

            return Ok(new
            {
                Message =
                    "Booking created successfully.",

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
            // -------------------------------------------------
            // GET EMPLOYEE ID
            // -------------------------------------------------

            if (!TryGetEmployeeId(out int employeeId))
            {
                return Unauthorized(new
                {
                    Message =
                        "Invalid token. Employee Id not found."
                });
            }

            // -------------------------------------------------
            // GET BOOKINGS
            // -------------------------------------------------

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
            // -------------------------------------------------
            // GET EMPLOYEE ID
            // -------------------------------------------------

            if (!TryGetEmployeeId(out int employeeId))
            {
                return Unauthorized(new
                {
                    Message =
                        "Invalid token. Employee Id not found."
                });
            }

            // -------------------------------------------------
            // GET RECENT RESERVATIONS
            // -------------------------------------------------

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
            // -------------------------------------------------
            // GET EMPLOYEE ID
            // -------------------------------------------------

            if (!TryGetEmployeeId(out int employeeId))
            {
                return Unauthorized(new
                {
                    Message =
                        "Invalid token. Employee Id not found."
                });
            }

            // -------------------------------------------------
            // GET BOOKING
            // -------------------------------------------------

            var booking =
                await _employeeBookingService
                    .GetBookingByIdAsync(
                        bookingId,
                        employeeId);

            // -------------------------------------------------
            // BOOKING NOT FOUND
            // -------------------------------------------------

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
            // -------------------------------------------------
            // GET EMPLOYEE ID
            // -------------------------------------------------

            if (!TryGetEmployeeId(out int employeeId))
            {
                return Unauthorized(new
                {
                    Message =
                        "Invalid token. Employee Id not found."
                });
            }

            // -------------------------------------------------
            // CANCEL BOOKING
            // -------------------------------------------------

            var result =
                await _employeeBookingService
                    .CancelBookingAsync(
                        bookingId,
                        employeeId);

            // -------------------------------------------------
            // BOOKING NOT FOUND
            // -------------------------------------------------

            if (!result)
            {
                return NotFound(new
                {
                    Message = "Booking not found."
                });
            }

            return Ok(new
            {
                Message =
                    "Booking cancelled successfully."
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
            // -------------------------------------------------
            // GET EMPLOYEE ID
            // -------------------------------------------------

            if (!TryGetEmployeeId(out int employeeId))
            {
                return Unauthorized(new
                {
                    Message =
                        "Invalid token. Employee Id not found."
                });
            }

            // -------------------------------------------------
            // REQUEST REQUIRED
            // -------------------------------------------------

            if (request == null)
            {
                return BadRequest(new
                {
                    Message =
                        "Update booking request is required."
                });
            }

            // -------------------------------------------------
            // UPDATE BOOKING
            // -------------------------------------------------

            var result =
                await _employeeBookingService
                    .UpdateBookingAsync(
                        bookingId,
                        employeeId,
                        request);

            // -------------------------------------------------
            // BOOKING NOT FOUND
            // -------------------------------------------------

            if (!result)
            {
                return NotFound(new
                {
                    Message = "Booking not found."
                });
            }

            return Ok(new
            {
                Message =
                    "Booking updated successfully."
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
            // -------------------------------------------------
            // MODULE REQUIRED
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(module))
            {
                return BadRequest(new
                {
                    Message = "Module is required."
                });
            }

            // -------------------------------------------------
            // GET ROOMS
            // -------------------------------------------------

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
            // REQUEST REQUIRED
            // -------------------------------------------------

            if (request == null)
            {
                return BadRequest(new
                {
                    Message =
                        "Search request is required."
                });
            }

            // -------------------------------------------------
            // DATE VALIDATION
            // -------------------------------------------------

            if (request.BookingDate.HasValue)
            {
                var bookingDate =
                    request.BookingDate.Value;

                var dateValidation =
                    ValidateBookingDate(bookingDate);

                if (dateValidation != null)
                {
                    return dateValidation;
                }
            }

            // -------------------------------------------------
            // TIME VALIDATION
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
            // -------------------------------------------------
            // GET EMPLOYEE ID
            // -------------------------------------------------

            if (!TryGetEmployeeId(out int employeeId))
            {
                return Unauthorized(new
                {
                    Message =
                        "Invalid token. Employee Id not found."
                });
            }

            // -------------------------------------------------
            // CHECK-IN
            // -------------------------------------------------

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