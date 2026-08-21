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

    // =========================================================
    // OFFICE HOURS
    // =========================================================
    // SpaceBook office booking/search hours:
    //
    // 10:00 AM - 07:30 PM
    // =========================================================

    private static readonly TimeOnly OfficeStartTime =
        new TimeOnly(10, 0);

    private static readonly TimeOnly OfficeEndTime =
        new TimeOnly(19, 0);

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

        if (bookingDate.DayOfWeek ==
            DayOfWeek.Saturday)
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

        if (bookingDate.DayOfWeek ==
            DayOfWeek.Sunday)
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
    // VALIDATE TIME RANGE
    // =========================================================

    private IActionResult? ValidateTimeRange(
        TimeOnly startTime,
        TimeOnly endTime,
        bool checkCurrentTime = false,
        DateOnly? bookingDate = null)
    {
        // -----------------------------------------------------
        // START BEFORE END
        // -----------------------------------------------------

        if (startTime >= endTime)
        {
            return BadRequest(new
            {
                Message =
                    "Start time must be earlier than end time."
            });
        }

        // -----------------------------------------------------
        // OFFICE START TIME
        // -----------------------------------------------------

        if (startTime < OfficeStartTime)
        {
            return BadRequest(new
            {
                Message =
                    "Time must start from 10:00 AM."
            });
        }

        // -----------------------------------------------------
        // OFFICE END TIME
        // -----------------------------------------------------

        if (endTime > OfficeEndTime)
        {
            return BadRequest(new
            {
                Message =
                    "Time must end by 07:30 PM."
            });
        }

        // -----------------------------------------------------
        // SAME DAY PAST TIME
        // -----------------------------------------------------

        if (checkCurrentTime &&
            bookingDate.HasValue &&
            bookingDate.Value ==
            DateOnly.FromDateTime(DateTime.Now))
        {
            var currentTime =
                TimeOnly.FromDateTime(DateTime.Now);

            if (startTime <= currentTime)
            {
                return BadRequest(new
                {
                    Message =
                        "Cannot use a time that has already passed."
                });
            }
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
            if (!TryGetEmployeeId(
                out int employeeId))
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Message =
                    "Unable to load employee dashboard.",
                Error =
                    ex.Message
            });
        }
    }

    // =========================================================
    // EMPLOYEE AVAILABILITY
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
                    Message =
                        "Date is required."
                });
            }

            // -------------------------------------------------
            // DATE VALIDATION
            // -------------------------------------------------

            var dateValidation =
                ValidateBookingDate(
                    date.Value);

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
                    Message =
                        "Invalid room type."
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Message =
                    "Unable to retrieve room availability.",
                Error =
                    ex.Message
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
            // EMPLOYEE
            // -------------------------------------------------

            if (!TryGetEmployeeId(
                out int employeeId))
            {
                return Unauthorized(new
                {
                    Message =
                        "Invalid token. Employee Id not found."
                });
            }

            // -------------------------------------------------
            // REQUEST
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
            // DATE
            // -------------------------------------------------

            var dateValidation =
                ValidateBookingDate(
                    request.BookingDate);

            if (dateValidation != null)
            {
                return dateValidation;
            }

            // -------------------------------------------------
            // TIME
            // -------------------------------------------------

            var timeValidation =
                ValidateTimeRange(
                    request.StartTime,
                    request.EndTime,
                    true,
                    request.BookingDate);

            if (timeValidation != null)
            {
                return timeValidation;
            }

            // -------------------------------------------------
            // PARTICIPANTS
            // -------------------------------------------------

            if (request.ParticipantCount <= 0)
            {
                return BadRequest(new
                {
                    Message =
                        "Participant count must be at least 1."
                });
            }

            // -------------------------------------------------
            // ROOM
            // -------------------------------------------------

            if (request.RoomId <= 0)
            {
                return BadRequest(new
                {
                    Message =
                        "Room ID is required."
                });
            }

            // -------------------------------------------------
            // CREATE
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
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Message =
                    "Unable to create booking.",
                Error =
                    ex.InnerException?.Message ??
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
            if (!TryGetEmployeeId(
                out int employeeId))
            {
                return Unauthorized(new
                {
                    Message =
                        "Invalid token. Employee Id not found."
                });
            }

            var result =
                await _dashboardService
                    .GetMyBookingsAsync(
                        employeeId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Message =
                    "Unable to retrieve bookings.",
                Error =
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
            if (!TryGetEmployeeId(
                out int employeeId))
            {
                return Unauthorized(new
                {
                    Message =
                        "Invalid token. Employee Id not found."
                });
            }

            var result =
                await _dashboardService
                    .GetRecentReservationsAsync(
                        employeeId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Message =
                    "Unable to retrieve recent reservations.",
                Error =
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
            // -------------------------------------------------
            // EMPLOYEE
            // -------------------------------------------------

            if (!TryGetEmployeeId(
                out int employeeId))
            {
                return Unauthorized(new
                {
                    Message =
                        "Invalid token. Employee Id not found."
                });
            }

            // -------------------------------------------------
            // BOOKING ID
            // -------------------------------------------------

            if (bookingId <= 0)
            {
                return BadRequest(new
                {
                    Message =
                        "Invalid booking ID."
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Message =
                    "Unable to retrieve booking.",
                Error =
                    ex.Message
            });
        }
    }

    // =========================================================
    // CANCEL BOOKING
    // =========================================================

    [HttpPut("bookings/{bookingId:int}/cancel")]
    public async Task<IActionResult> CancelBooking(
        int bookingId,
        [FromBody] CancelBookingRequestDto request)
    {
        try
        {
            // -------------------------------------------------
            // EMPLOYEE
            // -------------------------------------------------

            if (!TryGetEmployeeId(
                out int employeeId))
            {
                return Unauthorized(new
                {
                    Message =
                        "Invalid token. Employee Id not found."
                });
            }

            // -------------------------------------------------
            // BOOKING ID
            // -------------------------------------------------

            if (bookingId <= 0)
            {
                return BadRequest(new
                {
                    Message =
                        "Invalid booking ID."
                });
            }

            // -------------------------------------------------
            // REQUEST
            // -------------------------------------------------

            if (request == null)
            {
                return BadRequest(new
                {
                    Message =
                        "Cancellation request is required."
                });
            }

            // -------------------------------------------------
            // REASON
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                request.Reason))
            {
                return BadRequest(new
                {
                    Message =
                        "Cancellation reason is required."
                });
            }

            var reason =
                request.Reason.Trim();

            if (reason.Length > 500)
            {
                return BadRequest(new
                {
                    Message =
                        "Cancellation reason cannot exceed 500 characters."
                });
            }

            // -------------------------------------------------
            // CANCEL
            // -------------------------------------------------

            var result =
                await _employeeBookingService
                    .CancelBookingAsync(
                        bookingId,
                        employeeId,
                        reason);

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
                    "Booking cancelled successfully.",

                BookingId =
                    bookingId,

                Status =
                    "Cancelled",

                CancellationReason =
                    reason
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Message =
                    "Unable to cancel booking.",
                Error =
                    ex.InnerException?.Message ??
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
            // -------------------------------------------------
            // EMPLOYEE
            // -------------------------------------------------

            if (!TryGetEmployeeId(
                out int employeeId))
            {
                return Unauthorized(new
                {
                    Message =
                        "Invalid token. Employee Id not found."
                });
            }

            // -------------------------------------------------
            // BOOKING ID
            // -------------------------------------------------

            if (bookingId <= 0)
            {
                return BadRequest(new
                {
                    Message =
                        "Invalid booking ID."
                });
            }

            // -------------------------------------------------
            // REQUEST
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
            // DATE
            // -------------------------------------------------

            var dateValidation =
                ValidateBookingDate(
                    request.BookingDate);

            if (dateValidation != null)
            {
                return dateValidation;
            }

            // -------------------------------------------------
            // TIME
            // -------------------------------------------------

            var timeValidation =
                ValidateTimeRange(
                    request.StartTime,
                    request.EndTime,
                    true,
                    request.BookingDate);

            if (timeValidation != null)
            {
                return timeValidation;
            }

            // -------------------------------------------------
            // PARTICIPANT COUNT
            // -------------------------------------------------

            if (request.ParticipantCount <= 0)
            {
                return BadRequest(new
                {
                    Message =
                        "Participant count must be greater than zero."
                });
            }

            // -------------------------------------------------
            // ROOM
            // -------------------------------------------------

            if (!request.RoomId.HasValue ||
                request.RoomId.Value <= 0)
            {
                return BadRequest(new
                {
                    Message =
                        "Room ID is required."
                });
            }

            // -------------------------------------------------
            // UPDATE
            // -------------------------------------------------

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
                    "Booking updated successfully.",

                BookingId =
                    bookingId,

                Status =
                    "Pending"
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Message =
                    "Unable to update booking.",
                Error =
                    ex.InnerException?.Message ??
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
            // -------------------------------------------------
            // MODULE
            // -------------------------------------------------

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
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Message =
                    "Unable to retrieve rooms.",
                Error =
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
            // REQUEST
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
            // DETERMINE SEARCH CRITERIA
            // -------------------------------------------------

            var hasModule =
                !string.IsNullOrWhiteSpace(
                    request.Module);

            var hasRoomType =
                request.RoomTypeId.HasValue &&
                request.RoomTypeId.Value > 0;

            var hasParticipantCount =
                request.ParticipantCount.HasValue &&
                request.ParticipantCount.Value > 0;

            var hasBookingDate =
                request.BookingDate.HasValue;

            var hasStartTime =
                request.StartTime.HasValue;

            var hasEndTime =
                request.EndTime.HasValue;

            var hasFacilities =
                request.FacilityIds != null &&
                request.FacilityIds.Any(
                    id => id > 0);

            // -------------------------------------------------
            // AT LEAST ONE CRITERION
            // -------------------------------------------------

            if (!hasModule &&
                !hasRoomType &&
                !hasParticipantCount &&
                !hasBookingDate &&
                !hasStartTime &&
                !hasEndTime &&
                !hasFacilities)
            {
                return BadRequest(new
                {
                    Message =
                        "Please provide at least one search criterion."
                });
            }

            // -------------------------------------------------
            // PARTICIPANT COUNT
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
            // ROOM TYPE
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
            // DATE
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
            // START / END TIME
            // -------------------------------------------------

            if (hasStartTime != hasEndTime)
            {
                return BadRequest(new
                {
                    Message =
                        "Both start time and end time are required when searching by time."
                });
            }

            // -------------------------------------------------
            // TIME RANGE
            // -------------------------------------------------

            if (hasStartTime &&
                hasEndTime)
            {
                var timeValidation =
                    ValidateTimeRange(
                        request.StartTime!.Value,
                        request.EndTime!.Value,
                        request.BookingDate.HasValue,
                        request.BookingDate);

                if (timeValidation != null)
                {
                    return timeValidation;
                }
            }

            // -------------------------------------------------
            // FACILITY IDS
            // -------------------------------------------------

            if (request.FacilityIds != null)
            {
                if (request.FacilityIds.Any(id => id < 0))
                {
                    return BadRequest(new
                    {
                        Message =
                            "Facility IDs cannot be negative."
                    });
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
                rooms?.ToList()
                ?? new List<AvailableRoomDto>();

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
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Message =
                    "Unable to search available rooms.",
                Error =
                    ex.InnerException?.Message ??
                    ex.Message
            });
        }
    }

    // =========================================================
    // CHECK-IN
    // =========================================================

    [HttpPost("bookings/{bookingId:int}/checkin")]
    public async Task<IActionResult> CheckIn(
        int bookingId)
    {
        try
        {
            // -------------------------------------------------
            // EMPLOYEE
            // -------------------------------------------------

            if (!TryGetEmployeeId(
                out int employeeId))
            {
                return Unauthorized(new
                {
                    Message =
                        "Invalid token. Employee Id not found."
                });
            }

            // -------------------------------------------------
            // BOOKING ID
            // -------------------------------------------------

            if (bookingId <= 0)
            {
                return BadRequest(new
                {
                    Message =
                        "Invalid booking ID."
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
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Message =
                    "Unable to check in.",
                Error =
                    ex.InnerException?.Message ??
                    ex.Message
            });
        }
    }
}