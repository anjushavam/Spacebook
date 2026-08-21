using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Hotseat;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
using SpaceBook.Infrastructure.Data;
using System.Security.Claims;

namespace SpaceBook.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class HotseatController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationRepository _notificationRepository;

    public HotseatController(
        ApplicationDbContext context,
        INotificationRepository notificationRepository)
    {
        _context = context;
        _notificationRepository = notificationRepository;
    }

    // ============================================================
    // CREATE NOTIFICATION HELPER
    // ============================================================

    private async Task CreateHotseatNotificationAsync(
        int employeeId,
        int hotseatBookingId,
        string message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (message.Length > 500)
            {
                message = message[..500];
            }

            var notification = new Notification
            {
                EmployeeId = employeeId,

                // Normal room booking does not apply
                BookingId = null,

                // Hotseat booking reference
                HotseatBookingId = hotseatBookingId,

                Message = message,

                IsRead = false,

                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository
                .AddAsync(notification);

            await _notificationRepository
                .SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Notification failure should not fail
            // the hotseat operation itself.
            Console.WriteLine(
                $"[HotseatController] Notification creation failed " +
                $"for hotseat booking {hotseatBookingId}.");

            Console.WriteLine(
                $"[HotseatController] Exception: {ex}");
        }
    }

    // ============================================================
    // GET: api/Hotseat
    // ============================================================

    [HttpGet]
    public async Task<ActionResult<IEnumerable<HotseatSeatDto>>>
        GetOfficeMap(
            [FromQuery] string? date,
            [FromQuery] string? city,
            [FromQuery] string? building,
            [FromQuery] string? module)
    {
        // --------------------------------------------------------
        // 1. PARSE DATE
        // --------------------------------------------------------

        DateOnly? bookingDate = null;

        if (!string.IsNullOrWhiteSpace(date))
        {
            if (DateOnly.TryParse(
                    date,
                    out var parsedDate))
            {
                bookingDate = parsedDate;
            }
            else
            {
                return BadRequest(new
                {
                    message =
                        "Invalid date format."
                });
            }
        }

        // --------------------------------------------------------
        // 2. GET ACTIVE SEATS
        // --------------------------------------------------------

        var seatsQuery =
            _context.Seats
                .AsNoTracking()
                .Include(s => s.Module)
                    .ThenInclude(m => m!.Office)
                    .ThenInclude(o => o!.Location)
                .Where(s => s.IsActive)
                .AsQueryable();

        // --------------------------------------------------------
        // 3. MODULE FILTER
        // --------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(module))
        {
            seatsQuery =
                seatsQuery.Where(s =>
                    s.Module != null &&
                    s.Module.ModuleName == module);
        }

        // --------------------------------------------------------
        // 4. BUILDING FILTER
        // --------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(building))
        {
            seatsQuery =
                seatsQuery.Where(s =>
                    s.Module != null &&
                    s.Module.Office != null &&
                    s.Module.Office.OfficeName ==
                        building);
        }

        // --------------------------------------------------------
        // 5. CITY FILTER
        // --------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(city))
        {
            seatsQuery =
                seatsQuery.Where(s =>
                    s.Module != null &&
                    s.Module.Office != null &&
                    s.Module.Office.Location != null &&
                    s.Module.Office.Location.LocationName ==
                        city);
        }

        // --------------------------------------------------------
        // 6. GET SEAT STATUS
        // --------------------------------------------------------

        var seats =
            await seatsQuery
                .OrderBy(s => s.ModuleId)
                .ThenBy(s => s.Section)
                .ThenBy(s => s.RowNumber)
                .ThenBy(s => s.ColumnNumber)
                .Select(s => new
                {
                    s.SeatId,

                    s.SeatNumber,

                    Section =
                        s.Section ?? "",

                    Row =
                        s.RowNumber,

                    IsBooked =
                        bookingDate.HasValue &&
                        s.HotseatBookings.Any(b =>
                            b.BookingDate ==
                                bookingDate.Value &&
                            (
                                b.BookingStatus ==
                                    "Confirmed" ||

                                b.BookingStatus ==
                                    "CheckedIn"
                            ))
                })
                .ToListAsync();

        // --------------------------------------------------------
        // 7. MAP
        // --------------------------------------------------------

        var result =
            seats.Select(s =>
                new HotseatSeatDto
                {
                    SeatNumber =
                        s.SeatNumber,

                    Section =
                        s.Section,

                    Row =
                        s.Row,

                    Status =
                        s.IsBooked
                            ? "Booked"
                            : "Vacant"
                })
                .ToList();

        return Ok(result);
    }

    // ============================================================
    // GET: api/Hotseat/my-bookings
    // ============================================================

    [HttpGet("my-bookings")]
    public async Task<IActionResult> GetMyBookings()
    {
        var employeeIdClaim =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(employeeIdClaim) ||
            !int.TryParse(
                employeeIdClaim,
                out int employeeId))
        {
            return Unauthorized(new
            {
                message =
                    "Employee information could not be determined."
            });
        }

        var bookings =
            await _context.HotseatBookings
                .AsNoTracking()

                .Where(b =>
                    b.EmployeeId == employeeId)

                .Include(b => b.Seat)
                    .ThenInclude(s => s.Module)

                .OrderByDescending(
                    b => b.BookingDate)

                .ThenByDescending(
                    b => b.BookedOn)

                .Select(b => new
                {
                    bookingId =
                        b.HotseatBookingId,

                    seatId =
                        b.SeatId,

                    seatNumber =
                        b.Seat != null
                            ? b.Seat.SeatNumber
                            : "",

                    module =
                        b.Seat != null &&
                        b.Seat.Module != null
                            ? b.Seat.Module.ModuleName
                            : "",

                    type =
                        "Hot Seat",

                    date =
                        b.BookingDate,

                    expectedCheckIn =
                        b.CheckInDeadline,

                    status =
                        b.BookingStatus,

                    bookedOn =
                        b.BookedOn,

                    checkInTime =
                        b.CheckInTime,

                    releasedOn =
                        b.ReleasedOn
                })

                .ToListAsync();

        return Ok(bookings);
    }

    // ============================================================
    // POST: api/Hotseat
    // CREATE HOTSEAT BOOKING
    // ============================================================

    [HttpPost]
    public async Task<IActionResult> CreateBooking(
        [FromBody] CreateHotseatBookingDto request)
    {
        if (request == null)
        {
            return BadRequest(new
            {
                message =
                    "Booking request is required."
            });
        }

        // --------------------------------------------------------
        // DATE VALIDATION
        // --------------------------------------------------------

        var todayUtc =
            DateOnly.FromDateTime(
                DateTime.UtcNow);

        if (request.BookingDate < todayUtc)
        {
            return BadRequest(new
            {
                message =
                    "Booking date cannot be in the past."
            });
        }

        // --------------------------------------------------------
        // GET SEAT
        // --------------------------------------------------------

        var seat =
            await _context.Seats
                .Include(s => s.Module)

                .FirstOrDefaultAsync(s =>
                    s.SeatId == request.SeatId &&
                    s.IsActive);

        if (seat == null)
        {
            return NotFound(new
            {
                message =
                    "Seat not found or inactive."
            });
        }

        // --------------------------------------------------------
        // GET EMPLOYEE
        // --------------------------------------------------------

        var employeeIdClaim =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(employeeIdClaim))
        {
            return Unauthorized(new
            {
                message =
                    "Employee information could not be determined."
            });
        }

        if (!int.TryParse(
                employeeIdClaim,
                out int employeeId))
        {
            return Unauthorized(new
            {
                message =
                    "Invalid employee ID."
            });
        }

        var employeeExists =
            await _context.Employees
                .AnyAsync(e =>
                    e.EmployeeId == employeeId &&
                    e.IsActive);

        if (!employeeExists)
        {
            return Unauthorized(new
            {
                message =
                    "Employee not found or inactive."
            });
        }

        // --------------------------------------------------------
        // CHECK SEAT ALREADY BOOKED
        // --------------------------------------------------------

        var existingBooking =
            await _context.HotseatBookings
                .AsNoTracking()

                .FirstOrDefaultAsync(b =>
                    b.SeatId == request.SeatId &&
                    b.BookingDate ==
                        request.BookingDate &&
                    (
                        b.BookingStatus ==
                            "Confirmed" ||

                        b.BookingStatus ==
                            "CheckedIn"
                    ));

        if (existingBooking != null)
        {
            return Conflict(new
            {
                message =
                    "This seat is already booked for the selected date.",

                existingBookingId =
                    existingBooking
                        .HotseatBookingId,

                bookingStatus =
                    existingBooking
                        .BookingStatus
            });
        }

        // --------------------------------------------------------
        // PREVENT EMPLOYEE DUPLICATE BOOKING
        // --------------------------------------------------------

        var employeeExistingBooking =
            await _context.HotseatBookings
                .AsNoTracking()

                .FirstOrDefaultAsync(b =>
                    b.EmployeeId == employeeId &&
                    b.BookingDate ==
                        request.BookingDate &&
                    (
                        b.BookingStatus ==
                            "Confirmed" ||

                        b.BookingStatus ==
                            "CheckedIn"
                    ));

        if (employeeExistingBooking != null)
        {
            return Conflict(new
            {
                message =
                    "You already have a hotseat booking for this date.",

                existingBookingId =
                    employeeExistingBooking
                        .HotseatBookingId,

                seatId =
                    employeeExistingBooking.SeatId,

                bookingStatus =
                    employeeExistingBooking
                        .BookingStatus
            });
        }

        // --------------------------------------------------------
        // CHECK-IN DEADLINE
        // --------------------------------------------------------

        var checkInTime =
            request.ExpectedCheckInTime ??
            new TimeOnly(9, 0, 0);

        var checkInDeadlineUtc =
            DateTime.SpecifyKind(
                request.BookingDate
                    .ToDateTime(checkInTime),
                DateTimeKind.Utc);

        // --------------------------------------------------------
        // CREATE BOOKING
        // --------------------------------------------------------

        var booking =
            new HotseatBooking
            {
                SeatId =
                    request.SeatId,

                EmployeeId =
                    employeeId,

                BookingDate =
                    request.BookingDate,

                BookingStatus =
                    "Confirmed",

                BookedOn =
                    DateTime.UtcNow,

                CheckInDeadline =
                    checkInDeadlineUtc,

                CheckInTime =
                    null,

                ReleasedOn =
                    null,

                RecordIngestedBy =
                    employeeId.ToString(),

                RecordIngestedOn =
                    DateTime.UtcNow,

                RecordModifiedBy =
                    null,

                RecordModifiedOn =
                    null
            };

        // --------------------------------------------------------
        // SAVE BOOKING
        // --------------------------------------------------------

        try
        {
            _context.HotseatBookings
                .Add(booking);

            await _context
                .SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine(
                "HOTSEAT BOOKING DATABASE ERROR:");

            Console.WriteLine(
                ex.ToString());

            return StatusCode(
                500,
                new
                {
                    message =
                        "An error occurred while saving the hotseat booking.",

                    detail =
                        ex.InnerException?.Message ??
                        ex.Message
                });
        }

        // --------------------------------------------------------
        // CREATE CONFIRMATION NOTIFICATION
        // --------------------------------------------------------

        await CreateHotseatNotificationAsync(
            employeeId,
            booking.HotseatBookingId,
            $"Your hotseat booking for {seat.SeatNumber} " +
            $"on {booking.BookingDate:dd-MMM-yyyy} has been confirmed.");

        // --------------------------------------------------------
        // RETURN
        // --------------------------------------------------------

        return Ok(new
        {
            message =
                "Hotseat booked successfully.",

            bookingId =
                booking.HotseatBookingId,

            seatId =
                booking.SeatId,

            seatNumber =
                seat.SeatNumber,

            employeeId =
                booking.EmployeeId,

            bookingDate =
                booking.BookingDate,

            bookingStatus =
                booking.BookingStatus,

            bookedOn =
                booking.BookedOn,

            checkInDeadline =
                booking.CheckInDeadline
        });
    }

    // ============================================================
    // PUT: api/Hotseat/{id}
    // UPDATE HOTSEAT BOOKING
    // ============================================================

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateBooking(
        int id,
        [FromBody] CreateHotseatBookingDto request)
    {
        if (request == null)
        {
            return BadRequest(new
            {
                message =
                    "Booking request is required."
            });
        }

        var employeeIdClaim =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(employeeIdClaim) ||
            !int.TryParse(
                employeeIdClaim,
                out int employeeId))
        {
            return Unauthorized(new
            {
                message =
                    "Employee information could not be determined."
            });
        }

        // --------------------------------------------------------
        // GET BOOKING
        // --------------------------------------------------------

        var booking =
            await _context.HotseatBookings
                .FirstOrDefaultAsync(b =>
                    b.HotseatBookingId == id);

        if (booking == null)
        {
            return NotFound(new
            {
                message =
                    "Hotseat booking not found."
            });
        }

        if (booking.EmployeeId != employeeId)
        {
            return Forbid();
        }

        if (!string.Equals(
                booking.BookingStatus,
                "Confirmed",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message =
                    "Only confirmed bookings can be edited."
            });
        }

        // --------------------------------------------------------
        // DATE VALIDATION
        // --------------------------------------------------------

        var todayUtc =
            DateOnly.FromDateTime(
                DateTime.UtcNow);

        if (request.BookingDate < todayUtc)
        {
            return BadRequest(new
            {
                message =
                    "Booking date cannot be in the past."
            });
        }

        // --------------------------------------------------------
        // GET NEW SEAT
        // --------------------------------------------------------

        var seat =
            await _context.Seats
                .Include(s => s.Module)

                .FirstOrDefaultAsync(s =>
                    s.SeatId == request.SeatId &&
                    s.IsActive);

        if (seat == null)
        {
            return NotFound(new
            {
                message =
                    "Seat not found or inactive."
            });
        }

        // --------------------------------------------------------
        // CHECK SEAT AVAILABILITY
        // --------------------------------------------------------

        var seatAlreadyBooked =
            await _context.HotseatBookings
                .AsNoTracking()

                .AnyAsync(b =>
                    b.HotseatBookingId != id &&
                    b.SeatId == request.SeatId &&
                    b.BookingDate ==
                        request.BookingDate &&
                    (
                        b.BookingStatus ==
                            "Confirmed" ||

                        b.BookingStatus ==
                            "CheckedIn"
                    ));

        if (seatAlreadyBooked)
        {
            return Conflict(new
            {
                message =
                    "This seat is already booked for the selected date."
            });
        }

        // --------------------------------------------------------
        // CHECK EMPLOYEE DUPLICATE
        // --------------------------------------------------------

        var employeeAlreadyBooked =
            await _context.HotseatBookings
                .AsNoTracking()

                .AnyAsync(b =>
                    b.HotseatBookingId != id &&
                    b.EmployeeId == employeeId &&
                    b.BookingDate ==
                        request.BookingDate &&
                    (
                        b.BookingStatus ==
                            "Confirmed" ||

                        b.BookingStatus ==
                            "CheckedIn"
                    ));

        if (employeeAlreadyBooked)
        {
            return Conflict(new
            {
                message =
                    "You already have another hotseat booking for this date."
            });
        }

        // --------------------------------------------------------
        // CHECK-IN DEADLINE
        // --------------------------------------------------------

        var checkInTime =
            request.ExpectedCheckInTime ??
            new TimeOnly(9, 0, 0);

        var checkInDeadlineUtc =
            DateTime.SpecifyKind(
                request.BookingDate
                    .ToDateTime(checkInTime),
                DateTimeKind.Utc);

        // --------------------------------------------------------
        // UPDATE
        // --------------------------------------------------------

        booking.SeatId =
            request.SeatId;

        booking.BookingDate =
            request.BookingDate;

        booking.CheckInDeadline =
            checkInDeadlineUtc;

        booking.RecordModifiedBy =
            employeeId.ToString();

        booking.RecordModifiedOn =
            DateTime.UtcNow;

        try
        {
            await _context
                .SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            return StatusCode(
                500,
                new
                {
                    message =
                        "An error occurred while updating the hotseat booking.",

                    detail =
                        ex.InnerException?.Message ??
                        ex.Message
                });
        }

        // --------------------------------------------------------
        // UPDATE NOTIFICATION
        // --------------------------------------------------------

        await CreateHotseatNotificationAsync(
            employeeId,
            booking.HotseatBookingId,
            $"Your hotseat booking has been updated to " +
            $"{seat.SeatNumber} on " +
            $"{booking.BookingDate:dd-MMM-yyyy}.");

        return Ok(new
        {
            message =
                "Hotseat booking updated successfully.",

            bookingId =
                booking.HotseatBookingId,

            seatId =
                booking.SeatId,

            seatNumber =
                seat.SeatNumber,

            module =
                seat.Module?.ModuleName,

            bookingDate =
                booking.BookingDate,

            bookingStatus =
                booking.BookingStatus,

            checkInDeadline =
                booking.CheckInDeadline,

            modifiedOn =
                booking.RecordModifiedOn
        });
    }

    // ============================================================
    // POST: api/Hotseat/{id}/check-in
    // ============================================================

    [HttpPost("{id:int}/check-in")]
    public async Task<IActionResult> CheckIn(
        int id)
    {
        var employeeIdClaim =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(employeeIdClaim) ||
            !int.TryParse(
                employeeIdClaim,
                out int employeeId))
        {
            return Unauthorized(new
            {
                message =
                    "Employee information could not be determined."
            });
        }

        // --------------------------------------------------------
        // FIND BOOKING
        // --------------------------------------------------------

        var booking =
            await _context.HotseatBookings
                .Include(b => b.Seat)

                .FirstOrDefaultAsync(b =>
                    b.HotseatBookingId == id);

        if (booking == null)
        {
            return NotFound(new
            {
                message =
                    "Hotseat booking not found."
            });
        }

        if (booking.EmployeeId != employeeId)
        {
            return Forbid();
        }

        // --------------------------------------------------------
        // STATUS VALIDATION
        // --------------------------------------------------------

        if (string.Equals(
                booking.BookingStatus,
                "CheckedIn",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message =
                    "You have already checked in to this hotseat."
            });
        }

        if (string.Equals(
                booking.BookingStatus,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message =
                    "Cancelled bookings cannot be checked in."
            });
        }

        if (string.Equals(
                booking.BookingStatus,
                "Released",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message =
                    "Released bookings cannot be checked in."
            });
        }

        if (string.Equals(
                booking.BookingStatus,
                "Expired",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message =
                    "Expired bookings cannot be checked in."
            });
        }

        if (!string.Equals(
                booking.BookingStatus,
                "Confirmed",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message =
                    "This booking cannot be checked in."
            });
        }

        // --------------------------------------------------------
        // DATE VALIDATION
        // --------------------------------------------------------

        var todayUtc =
            DateOnly.FromDateTime(
                DateTime.UtcNow);

        if (booking.BookingDate != todayUtc)
        {
            return BadRequest(new
            {
                message =
                    "You can only check in on the booking date."
            });
        }

        // --------------------------------------------------------
        // CHECK-IN
        // --------------------------------------------------------

        booking.BookingStatus =
            "CheckedIn";

        booking.CheckInTime =
            DateTime.UtcNow;

        booking.RecordModifiedBy =
            employeeId.ToString();

        booking.RecordModifiedOn =
            DateTime.UtcNow;

        try
        {
            await _context
                .SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            return StatusCode(
                500,
                new
                {
                    message =
                        "An error occurred while checking in.",

                    detail =
                        ex.InnerException?.Message ??
                        ex.Message
                });
        }

        // --------------------------------------------------------
        // CHECK-IN NOTIFICATION
        // --------------------------------------------------------

        var seatNumber =
            booking.Seat?.SeatNumber ??
            $"Seat {booking.SeatId}";

        await CreateHotseatNotificationAsync(
            employeeId,
            booking.HotseatBookingId,
            $"You have successfully checked in to {seatNumber}.");

        return Ok(new
        {
            message =
                "Checked in successfully.",

            bookingId =
                booking.HotseatBookingId,

            seatId =
                booking.SeatId,

            seatNumber =
                booking.Seat?.SeatNumber,

            bookingDate =
                booking.BookingDate,

            bookingStatus =
                booking.BookingStatus,

            checkInTime =
                booking.CheckInTime
        });
    }

    // ============================================================
    // DELETE: api/Hotseat/{id}
    // SOFT CANCEL HOTSEAT BOOKING
    // ============================================================

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> CancelBooking(
        int id)
    {
        var employeeIdClaim =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(employeeIdClaim) ||
            !int.TryParse(
                employeeIdClaim,
                out int employeeId))
        {
            return Unauthorized(new
            {
                message =
                    "Employee information could not be determined."
            });
        }

        // --------------------------------------------------------
        // GET BOOKING + SEAT
        // --------------------------------------------------------

        var booking =
            await _context.HotseatBookings
                .Include(b => b.Seat)

                .FirstOrDefaultAsync(b =>
                    b.HotseatBookingId == id);

        if (booking == null)
        {
            return NotFound(new
            {
                message =
                    "Hotseat booking not found."
            });
        }

        if (booking.EmployeeId != employeeId)
        {
            return Forbid();
        }

        // --------------------------------------------------------
        // STATUS VALIDATION
        // --------------------------------------------------------

        if (string.Equals(
                booking.BookingStatus,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message =
                    "This hotseat booking is already cancelled."
            });
        }

        if (string.Equals(
                booking.BookingStatus,
                "Released",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message =
                    "This hotseat booking has already been released."
            });
        }

        if (string.Equals(
                booking.BookingStatus,
                "Expired",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message =
                    "This hotseat booking has expired."
            });
        }

        // --------------------------------------------------------
        // CANCEL
        // --------------------------------------------------------

        booking.BookingStatus =
            "Cancelled";

        booking.RecordModifiedBy =
            employeeId.ToString();

        booking.RecordModifiedOn =
            DateTime.UtcNow;

        try
        {
            await _context
                .SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            return StatusCode(
                500,
                new
                {
                    message =
                        "An error occurred while cancelling the hotseat booking.",

                    detail =
                        ex.InnerException?.Message ??
                        ex.Message
                });
        }

        // --------------------------------------------------------
        // CANCELLATION NOTIFICATION
        // --------------------------------------------------------

        var seatNumber =
            booking.Seat?.SeatNumber ??
            $"Seat {booking.SeatId}";

        await CreateHotseatNotificationAsync(
            employeeId,
            booking.HotseatBookingId,
            $"Your hotseat booking for {seatNumber} " +
            $"on {booking.BookingDate:dd-MMM-yyyy} has been cancelled. " +
            $"Reason: You cancelled the booking.");

        return Ok(new
        {
            message =
                "Hotseat booking cancelled successfully.",

            bookingId =
                booking.HotseatBookingId,

            seatId =
                booking.SeatId,

            employeeId =
                booking.EmployeeId,

            bookingDate =
                booking.BookingDate,

            bookingStatus =
                booking.BookingStatus,

            modifiedOn =
                booking.RecordModifiedOn
        });
    }
}