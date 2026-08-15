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

    private IActionResult? ValidateBookingDate(
        DateOnly bookingDate)
    {
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
                    $"Cannot use a past date. Today is {today:yyyy-MM-dd}."
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
                    "Room availability and bookings are not allowed on Saturdays."
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
                    "Room availability and bookings are not allowed on Sundays."
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
            if (!TryGetEmployeeId(out int employeeId))
            {
                return Unauthorized(new
                {
                    Message =
                        "Invalid token. Employee Id not found."
                });
            }

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

            // -------------------------------------------------
            // DATE VALIDATION
            // -------------------------------------------------

            var dateValidation =
                ValidateBookingDate(date.Value);

            if (dateValidation != null)
            {
                return dateValidation;
            }

            // -------------------------------------------------
            // ROOM TYPE VALIDATION
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
            // -------------------------------------------------

            var result =
                await _dashboardService
                    .GetAvailabilityAsync(
                        date.Value,
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
            // EMPLOYEE ID
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
            // REQUEST VALIDATION
            // -------------------------------------------------

            if (request == null)
            {
                return BadRequest(new
                {
                    Message =
                        "Booking request is required."
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

                BookingId =
                    bookingId,

                Status =
                    "Pending"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Message =
                    ex.Message
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
            if (!TryGetEmployeeId(out int employeeId))
            {
                return Unauthorized(new
                {
                    Message =
                        "Invalid token. Employee Id not found."
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
                Message =
                    ex.Message
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
            if (!TryGetEmployeeId(out int employeeId))
            {
                return Unauthorized(new
                {
                    Message =
                        "Invalid token. Employee Id not found."
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
                Message =
                    ex.Message
            });
        }
    }

    // =========================================================
    // VIEW BOOKING
    // =========================================================

    [HttpGet("bookings/{bookingId:int}")]
    public async Task<IActionResult> GetBookingById(
        int bookingId)
    {
        try
        {
            if (!TryGetEmployeeId(out int employeeId))
            {
                return Unauthorized(new
                {
                    Message =
                        "Invalid token. Employee Id not found."
                });
            }

            if (bookingId <= 0)
            {
                return BadRequest(new
                {
                    Message =
                        "Invalid booking ID."
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
                    Message =
                        "Booking not found."
                });
            }

            return Ok(booking);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Message =
                    ex.Message
            });
        }
    }

    // =========================================================
    // CANCEL BOOKING
    // =========================================================

    [HttpPut("bookings/{bookingId:int}/cancel")]
    public async Task<IActionResult> CancelBooking(
        int bookingId)
    {
        try
        {
            if (!TryGetEmployeeId(out int employeeId))
            {
                return Unauthorized(new
                {
                    Message =
                        "Invalid token. Employee Id not found."
                });
            }

            if (bookingId <= 0)
            {
                return BadRequest(new
                {
                    Message =
                        "Invalid booking ID."
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
                    Message =
                        "Booking not found."
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
                Message =
                    ex.Message
            });
        }
    }

    // =========================================================
    // UPDATE / RESCHEDULE BOOKING
    // =========================================================

    [HttpPut("bookings/{bookingId:int}")]
    public async Task<IActionResult> UpdateBooking(
        int bookingId,
        [FromBody] UpdateBookingRequestDto request)
    {
        try
        {
            if (!TryGetEmployeeId(out int employeeId))
            {
                return Unauthorized(new
                {
                    Message =
                        "Invalid token. Employee Id not found."
                });
            }

            if (bookingId <= 0)
            {
                return BadRequest(new
                {
                    Message =
                        "Invalid booking ID."
                });
            }

            if (request == null)
            {
                return BadRequest(new
                {
                    Message =
                        "Update booking request is required."
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
                    Message =
                        "Booking not found."
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
                Message =
                    ex.Message
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
                    Message =
                        "Module is required."
                });
            }

            var rooms =
                await _employeeBookingService
                    .GetRoomsByModuleAsync(
                        module.Trim());

            return Ok(rooms);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Message =
                    ex.Message
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
            // PARTICIPANT COUNT VALIDATION
            // -------------------------------------------------

            if (request.ParticipantCount.HasValue &&
                request.ParticipantCount.Value <= 0)
            {
                return BadRequest(new
                {
                    Message =
                        "Participant count must be greater than zero."
                });
            }

            // -------------------------------------------------
            // ROOM TYPE VALIDATION
            // -------------------------------------------------

            if (request.RoomTypeId.HasValue &&
                request.RoomTypeId.Value <= 0)
            {
                return BadRequest(new
                {
                    Message =
                        "Invalid room type."
                });
            }

            // -------------------------------------------------
            // DATE VALIDATION
            // -------------------------------------------------

            if (request.BookingDate.HasValue)
            {
                var dateValidation =
                    ValidateBookingDate(
                        request.BookingDate.Value);

                if (dateValidation != null)
                {
                    return dateValidation;
                }
            }

            // -------------------------------------------------
            // START / END TIME MUST BE PROVIDED TOGETHER
            // -------------------------------------------------

            var hasStartTime =
                request.StartTime.HasValue;

            var hasEndTime =
                request.EndTime.HasValue;

            if (hasStartTime != hasEndTime)
            {
                return BadRequest(new
                {
                    Message =
                        "Both start time and end time are required when searching by time."
                });
            }

            // -------------------------------------------------
            // TIME RANGE VALIDATION
            // -------------------------------------------------

            if (hasStartTime && hasEndTime)
            {
                if (request.StartTime!.Value >=
                    request.EndTime!.Value)
                {
                    return BadRequest(new
                    {
                        Message =
                            "Start time must be earlier than end time."
                    });
                }

                var officeStart =
                    new TimeOnly(9, 0);

                var officeEnd =
                    new TimeOnly(19, 30);

                if (request.StartTime.Value <
                    officeStart ||
                    request.EndTime.Value >
                    officeEnd)
                {
                    return BadRequest(new
                    {
                        Message =
                            "Search time must be between 09:00 AM and 07:30 PM."
                    });
                }

                // -------------------------------------------------
                // TODAY + PAST TIME
                // -------------------------------------------------

                if (request.BookingDate.HasValue &&
                    request.BookingDate.Value ==
                    DateOnly.FromDateTime(DateTime.Now))
                {
                    var currentTime =
                        TimeOnly.FromDateTime(DateTime.Now);

                    if (request.StartTime.Value <=
                        currentTime)
                    {
                        return BadRequest(new
                        {
                            Message =
                                "Cannot search for a time that has already passed."
                        });
                    }
                }
            }

            // -------------------------------------------------
            // SEARCH
            // -------------------------------------------------

            var rooms =
                await _employeeBookingService
                    .SearchAvailableRoomsAsync(
                        request);

            var roomList =
                rooms?.ToList() ??
                new List<AvailableRoomDto>();

            // -------------------------------------------------
            // NO ROOMS
            // -------------------------------------------------

            if (!roomList.Any())
            {
                if (request.ParticipantCount.HasValue)
                {
                    return Ok(new
                    {
                        Message =
                            "No room can accommodate the selected number of participants.",

                        Rooms =
                            roomList
                    });
                }

                return Ok(new
                {
                    Message =
                        "No rooms are available for the selected criteria.",

                    Rooms =
                        roomList
                });
            }

            // -------------------------------------------------
            // ROOMS FOUND
            // -------------------------------------------------

            return Ok(new
            {
                Message =
                    "Rooms found successfully.",

                Rooms =
                    roomList
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Message =
                    ex.Message
            });
        }
    }

    // =========================================================
    // CHECK-IN BOOKING
    // =========================================================

    [HttpPost("bookings/{bookingId:int}/checkin")]
    public async Task<IActionResult> CheckIn(
        int bookingId)
    {
        try
        {
            if (!TryGetEmployeeId(out int employeeId))
            {
                return Unauthorized(new
                {
                    Message =
                        "Invalid token. Employee Id not found."
                });
            }

            if (bookingId <= 0)
            {
                return BadRequest(new
                {
                    Message =
                        "Invalid booking ID."
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
                Message =
                    ex.Message
            });
        }
    }
}
