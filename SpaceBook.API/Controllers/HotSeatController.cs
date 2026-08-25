using Microsoft.AspNetCore.Authorization;
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
[Authorize(Roles = "Employee")]
public class HotseatController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationRepository _notificationRepository;

    private static readonly TimeZoneInfo IndiaTimeZone = GetIndiaTimeZone();

    private static TimeZoneInfo GetIndiaTimeZone()
    {
        try
        {
            // Linux / Render
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                // Windows
                return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }
        catch (InvalidTimeZoneException)
        {
            try
            {
                // Windows fallback
                return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }
    }

    private static DateTime GetIndiaNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            IndiaTimeZone);
    }

    private static DateOnly GetIndiaToday()
    {
        return DateOnly.FromDateTime(GetIndiaNow());
    }

    public HotseatController(
        ApplicationDbContext context,
        INotificationRepository notificationRepository)
    {
        _context = context;
        _notificationRepository = notificationRepository;
    }

    // ============================================================
    // CREATE HOTSEAT NOTIFICATION
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
                BookingId = null,
                HotseatBookingId = hotseatBookingId,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[HotseatController] Notification creation failed " +
                $"for hotseat booking {hotseatBookingId}.");

            Console.WriteLine(ex);
        }
    }

    // ============================================================
    // GET: api/Hotseat
    //
    // Examples:
    // /api/Hotseat
    // /api/Hotseat?date=2026-08-25
    // /api/Hotseat?module=Tidel OIS Module 1
    // /api/Hotseat?building=Tidel Park
    // /api/Hotseat?city=Chennai
    // ============================================================

    [HttpGet]
    public async Task<ActionResult<IEnumerable<HotseatSeatDto>>> GetOfficeMap(
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
            if (DateOnly.TryParse(date, out var parsedDate))
            {
                bookingDate = parsedDate;
            }
            else if (DateTime.TryParse(date, out var parsedDateTime))
            {
                bookingDate = DateOnly.FromDateTime(parsedDateTime);
            }
            else
            {
                return BadRequest(new
                {
                    message = "Invalid date format. Use yyyy-MM-dd."
                });
            }
        }

        // --------------------------------------------------------
        // 2. GET ACTIVE SEATS
        // --------------------------------------------------------

        var seatsQuery = _context.Seats
            .AsNoTracking()
            .Include(s => s.Module)
                .ThenInclude(m => m!.Office)
                    .ThenInclude(o => o!.Location)
            .Where(s => s.IsActive)
            .AsQueryable();

        // --------------------------------------------------------
        // 3. MODULE FILTER
        // Generic - works for ELCOT, Tidel OIS, etc.
        // --------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(module))
        {
            var trimmedModule = module.Trim().ToLower();
            seatsQuery = seatsQuery.Where(s =>
                s.Module != null &&
                s.Module.ModuleName.ToLower() == trimmedModule);
        }

        // --------------------------------------------------------
        // 4. BUILDING FILTER
        // --------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(building))
        {
            var trimmedBuilding = building.Trim().ToLower();
            seatsQuery = seatsQuery.Where(s =>
                s.Module != null &&
                s.Module.Office != null &&
                s.Module.Office.OfficeName.ToLower() == trimmedBuilding);
        }

        // --------------------------------------------------------
        // 5. CITY FILTER
        // --------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(city))
        {
            var trimmedCity = city.Trim().ToLower();
            seatsQuery = seatsQuery.Where(s =>
                s.Module != null &&
                s.Module.Office != null &&
                s.Module.Office.Location != null &&
                s.Module.Office.Location.LocationName.ToLower() == trimmedCity);
        }

        // --------------------------------------------------------
        // 6. GET SEATS + BOOKING STATUS
        // --------------------------------------------------------

        var seats = await seatsQuery
            .OrderBy(s => s.ModuleId)
            .ThenBy(s => s.Section)
            .ThenBy(s => s.RowNumber)
            .ThenBy(s => s.ColumnNumber)
            .Select(s => new
            {
                s.SeatId,

                s.SeatNumber,

                Section = s.Section ?? "",

                Row = s.RowNumber,

                IsBooked =
                    bookingDate.HasValue &&
                    s.HotseatBookings.Any(b =>
                        b.BookingDate == bookingDate.Value &&
                        (
                            b.BookingStatus == "Confirmed" ||
                            b.BookingStatus == "CheckedIn"
                        ))
            })
            .ToListAsync();

        // --------------------------------------------------------
        // 7. MAP RESPONSE
        // --------------------------------------------------------

        var result = seats
            .Select(s => new HotseatSeatDto
            {
                SeatId = s.SeatId,

                SeatNumber = s.SeatNumber,

                Section = s.Section,

                Row = s.Row,

                Status = s.IsBooked
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
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(employeeIdClaim) ||
            !int.TryParse(employeeIdClaim, out int employeeId))
        {
            return Unauthorized(new
            {
                message = "Employee information could not be determined."
            });
        }

        var bookings = await _context.HotseatBookings
            .AsNoTracking()
            .Where(b => b.EmployeeId == employeeId)
            .Include(b => b.Seat)
                .ThenInclude(s => s!.Module)
                    .ThenInclude(m => m!.Office)
                        .ThenInclude(o => o!.Location)
            .OrderByDescending(b => b.BookingDate)
            .ThenByDescending(b => b.BookedOn)
            .Select(b => new
            {
                bookingId = b.HotseatBookingId,
                hotseatBookingId = b.HotseatBookingId,

                seatId = b.SeatId,

                seatNumber =
                    b.Seat != null
                        ? b.Seat.SeatNumber
                        : "",

                module =
                    b.Seat != null &&
                    b.Seat.Module != null
                        ? b.Seat.Module.ModuleName
                        : "",

                moduleName =
                    b.Seat != null &&
                    b.Seat.Module != null
                        ? b.Seat.Module.ModuleName
                        : "",

                building =
                    b.Seat != null &&
                    b.Seat.Module != null &&
                    b.Seat.Module.Office != null
                        ? b.Seat.Module.Office.OfficeName
                        : "",

                city =
                    b.Seat != null &&
                    b.Seat.Module != null &&
                    b.Seat.Module.Office != null &&
                    b.Seat.Module.Office.Location != null
                        ? b.Seat.Module.Office.Location.LocationName
                        : "",

                type = "Hot Seat",

                date = b.BookingDate,
                bookingDate = b.BookingDate,

                expectedCheckIn = b.CheckInDeadline,
                checkInDeadline = b.CheckInDeadline,

                status = b.BookingStatus,
                bookingStatus = b.BookingStatus,

                bookedOn = b.BookedOn,
                bookedTime = b.BookedOn,

                checkInTime = b.CheckInTime,

                releasedOn = b.ReleasedOn
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
                message = "Booking request is required."
            });
        }

        // --------------------------------------------------------
        // VALIDATE DATE
        // --------------------------------------------------------

        if (request.BookingDate == default)
        {
            return BadRequest(new
            {
                message = "Booking date is required."
            });
        }

        var today = GetIndiaToday();

        if (request.BookingDate < today)
        {
            return BadRequest(new
            {
                message = "Booking date cannot be in the past."
            });
        }

        // --------------------------------------------------------
        // GET EMPLOYEE ID
        // --------------------------------------------------------

        var employeeIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(employeeIdClaim) ||
            !int.TryParse(employeeIdClaim, out int employeeId))
        {
            return Unauthorized(new
            {
                message = "Employee information could not be determined."
            });
        }

        // --------------------------------------------------------
        // VALIDATE EMPLOYEE
        // --------------------------------------------------------

        var employeeExists =
            await _context.Employees.AnyAsync(e =>
                e.EmployeeId == employeeId &&
                e.IsActive);

        if (!employeeExists)
        {
            return Unauthorized(new
            {
                message = "Employee not found or inactive."
            });
        }

        // --------------------------------------------------------
        // RESOLVE SEAT IF SEAT NUMBER WAS PROVIDED
        // --------------------------------------------------------

        if (request.SeatId <= 0 && !string.IsNullOrWhiteSpace(request.SeatNumber))
        {
            var seatQuery = _context.Seats
                .Include(s => s.Module)
                    .ThenInclude(m => m!.Office)
                        .ThenInclude(o => o!.Location)
                .Where(s =>
                    s.IsActive &&
                    s.SeatNumber.ToLower() == request.SeatNumber.Trim().ToLower());

            var moduleFilter = request.ModuleName ?? request.Module;
            if (!string.IsNullOrWhiteSpace(moduleFilter))
            {
                var trimmedModule = moduleFilter.Trim().ToLower();
                seatQuery = seatQuery.Where(s =>
                    s.Module != null &&
                    s.Module.ModuleName.ToLower() == trimmedModule);
            }

            var matchedSeat = await seatQuery.FirstOrDefaultAsync();
            if (matchedSeat != null)
            {
                request.SeatId = matchedSeat.SeatId;
            }
        }

        // --------------------------------------------------------
        // VALIDATE SEAT
        // --------------------------------------------------------

        var seat = await _context.Seats
            .Include(s => s.Module)
                .ThenInclude(m => m!.Office)
                    .ThenInclude(o => o!.Location)
            .FirstOrDefaultAsync(s =>
                s.SeatId == request.SeatId &&
                s.IsActive);

        if (seat == null)
        {
            return NotFound(new
            {
                message = "Seat not found or inactive."
            });
        }

        // --------------------------------------------------------
        // CHECK SEAT BOOKING
        // --------------------------------------------------------

        var existingBooking =
            await _context.HotseatBookings
                .AsNoTracking()
                .FirstOrDefaultAsync(b =>
                    b.SeatId == request.SeatId &&
                    b.BookingDate == request.BookingDate &&
                    (
                        b.BookingStatus == "Confirmed" ||
                        b.BookingStatus == "CheckedIn"
                    ));

        if (existingBooking != null)
        {
            return Conflict(new
            {
                message =
                    "This seat is already booked for the selected date.",

                existingBookingId =
                    existingBooking.HotseatBookingId,

                bookingStatus =
                    existingBooking.BookingStatus
            });
        }

        // --------------------------------------------------------
        // PREVENT EMPLOYEE DUPLICATE
        // --------------------------------------------------------

        var employeeExistingBooking =
            await _context.HotseatBookings
                .AsNoTracking()
                .FirstOrDefaultAsync(b =>
                    b.EmployeeId == employeeId &&
                    b.BookingDate == request.BookingDate &&
                    (
                        b.BookingStatus == "Confirmed" ||
                        b.BookingStatus == "CheckedIn"
                    ));

        if (employeeExistingBooking != null)
        {
            return Conflict(new
            {
                message =
                    "You already have a hotseat booking for this date.",

                existingBookingId =
                    employeeExistingBooking.HotseatBookingId,

                seatId =
                    employeeExistingBooking.SeatId,

                bookingStatus =
                    employeeExistingBooking.BookingStatus
            });
        }

        // --------------------------------------------------------
        // CHECK-IN DEADLINE
        // --------------------------------------------------------

        var expectedCheckInTime =
            request.ExpectedCheckInTime ??
            new TimeOnly(9, 0, 0);

        var localCheckInDateTime =
            request.BookingDate.ToDateTime(expectedCheckInTime);

        var checkInDeadlineUtc =
            TimeZoneInfo.ConvertTimeToUtc(
                localCheckInDateTime,
                IndiaTimeZone);

        // --------------------------------------------------------
        // CREATE BOOKING
        // --------------------------------------------------------

        var booking = new HotseatBooking
        {
            SeatId = request.SeatId,

            EmployeeId = employeeId,

            BookingDate = request.BookingDate,

            BookingStatus = "Confirmed",

            BookedOn = DateTime.UtcNow,

            CheckInDeadline = checkInDeadlineUtc,

            CheckInTime = null,

            ReleasedOn = null,

            RecordIngestedBy =
                employeeId.ToString(),

            RecordIngestedOn =
                DateTime.UtcNow,

            RecordModifiedBy = null,

            RecordModifiedOn = null
        };

        try
        {
            _context.HotseatBookings.Add(booking);

            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine(
                "HOTSEAT BOOKING DATABASE ERROR:");

            Console.WriteLine(ex);

            return StatusCode(500, new
            {
                message =
                    "An error occurred while saving the hotseat booking.",

                detail =
                    ex.InnerException?.Message ??
                    ex.Message
            });
        }

        // --------------------------------------------------------
        // NOTIFICATION
        // --------------------------------------------------------

        await CreateHotseatNotificationAsync(
            employeeId,
            booking.HotseatBookingId,

            $"Your hotseat booking for {seat.SeatNumber} " +
            $"on {booking.BookingDate:dd-MMM-yyyy} " +
            $"has been confirmed.");

        // --------------------------------------------------------
        // RESPONSE
        // --------------------------------------------------------

        return Ok(new
        {
            message = "Hotseat booked successfully.",

            bookingId =
                booking.HotseatBookingId,

            seatId =
                booking.SeatId,

            seatNumber =
                seat.SeatNumber,

            module =
                seat.Module?.ModuleName,

            building =
                seat.Module?.Office?.OfficeName,

            city =
                seat.Module?.Office?.Location?.LocationName,

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
                message = "Booking request is required."
            });
        }

        var employeeIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(employeeIdClaim) ||
            !int.TryParse(employeeIdClaim, out int employeeId))
        {
            return Unauthorized(new
            {
                message = "Employee information could not be determined."
            });
        }

        var booking =
            await _context.HotseatBookings
                .FirstOrDefaultAsync(b =>
                    b.HotseatBookingId == id);

        if (booking == null)
        {
            return NotFound(new
            {
                message = "Hotseat booking not found."
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

        if (request.BookingDate == default)
        {
            return BadRequest(new
            {
                message = "Booking date is required."
            });
        }

        var today = GetIndiaToday();

        if (request.BookingDate < today)
        {
            return BadRequest(new
            {
                message = "Booking date cannot be in the past."
            });
        }

        var seat =
            await _context.Seats
                .Include(s => s.Module)
                    .ThenInclude(m => m!.Office)
                        .ThenInclude(o => o!.Location)
                .FirstOrDefaultAsync(s =>
                    s.SeatId == request.SeatId &&
                    s.IsActive);

        if (seat == null)
        {
            return NotFound(new
            {
                message = "Seat not found or inactive."
            });
        }

        var seatAlreadyBooked =
            await _context.HotseatBookings
                .AsNoTracking()
                .AnyAsync(b =>
                    b.HotseatBookingId != id &&
                    b.SeatId == request.SeatId &&
                    b.BookingDate == request.BookingDate &&
                    (
                        b.BookingStatus == "Confirmed" ||
                        b.BookingStatus == "CheckedIn"
                    ));

        if (seatAlreadyBooked)
        {
            return Conflict(new
            {
                message =
                    "This seat is already booked for the selected date."
            });
        }

        var employeeAlreadyBooked =
            await _context.HotseatBookings
                .AsNoTracking()
                .AnyAsync(b =>
                    b.HotseatBookingId != id &&
                    b.EmployeeId == employeeId &&
                    b.BookingDate == request.BookingDate &&
                    (
                        b.BookingStatus == "Confirmed" ||
                        b.BookingStatus == "CheckedIn"
                    ));

        if (employeeAlreadyBooked)
        {
            return Conflict(new
            {
                message =
                    "You already have another hotseat booking for this date."
            });
        }

        var checkInTime =
            request.ExpectedCheckInTime ??
            new TimeOnly(9, 0, 0);

        var localCheckInDateTime =
            request.BookingDate.ToDateTime(checkInTime);

        var checkInDeadlineUtc =
            TimeZoneInfo.ConvertTimeToUtc(
                localCheckInDateTime,
                IndiaTimeZone);

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
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            return StatusCode(500, new
            {
                message =
                    "An error occurred while updating the hotseat booking.",

                detail =
                    ex.InnerException?.Message ??
                    ex.Message
            });
        }

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

            building =
                seat.Module?.Office?.OfficeName,

            city =
                seat.Module?.Office?.Location?.LocationName,

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
    public async Task<IActionResult> CheckIn(int id)
    {
        var employeeIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(employeeIdClaim) ||
            !int.TryParse(employeeIdClaim, out int employeeId))
        {
            return Unauthorized(new
            {
                message = "Employee information could not be determined."
            });
        }

        var booking =
            await _context.HotseatBookings
                .Include(b => b.Seat)
                    .ThenInclude(s => s!.Module)
                .FirstOrDefaultAsync(b =>
                    b.HotseatBookingId == id);

        if (booking == null)
        {
            return NotFound(new
            {
                message = "Hotseat booking not found."
            });
        }

        if (booking.EmployeeId != employeeId)
        {
            return Forbid();
        }

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

        var today = GetIndiaToday();

        if (booking.BookingDate != today)
        {
            return BadRequest(new
            {
                message =
                    "You can only check in on the booking date."
            });
        }

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
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            return StatusCode(500, new
            {
                message =
                    "An error occurred while checking in.",

                detail =
                    ex.InnerException?.Message ??
                    ex.Message
            });
        }

        var seatNumber =
            booking.Seat?.SeatNumber ??
            $"Seat {booking.SeatId}";

        await CreateHotseatNotificationAsync(
            employeeId,
            booking.HotseatBookingId,

            $"You have successfully checked in to " +
            $"{seatNumber}.");

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

            module =
                booking.Seat?.Module?.ModuleName,

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
    // CANCEL HOTSEAT BOOKING
    // ============================================================

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var employeeIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(employeeIdClaim) ||
            !int.TryParse(employeeIdClaim, out int employeeId))
        {
            return Unauthorized(new
            {
                message = "Employee information could not be determined."
            });
        }

        var booking =
            await _context.HotseatBookings
                .Include(b => b.Seat)
                    .ThenInclude(s => s!.Module)
                .FirstOrDefaultAsync(b =>
                    b.HotseatBookingId == id);

        if (booking == null)
        {
            return NotFound(new
            {
                message = "Hotseat booking not found."
            });
        }

        if (booking.EmployeeId != employeeId)
        {
            return Forbid();
        }

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

        booking.BookingStatus =
            "Cancelled";

        booking.RecordModifiedBy =
            employeeId.ToString();

        booking.RecordModifiedOn =
            DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            return StatusCode(500, new
            {
                message =
                    "An error occurred while cancelling the hotseat booking.",

                detail =
                    ex.InnerException?.Message ??
                    ex.Message
            });
        }

        var seatNumber =
            booking.Seat?.SeatNumber ??
            $"Seat {booking.SeatId}";

        await CreateHotseatNotificationAsync(
            employeeId,
            booking.HotseatBookingId,

            $"Your hotseat booking for {seatNumber} " +
            $"on {booking.BookingDate:dd-MMM-yyyy} " +
            $"has been cancelled. " +
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

    // ============================================================
    // GET: api/Hotseat/stats
    // ============================================================

    [HttpGet("stats")]
    public async Task<ActionResult<HotseatStatsDto>> GetStats()
    {
        var today = GetIndiaToday();

        // --------------------------------------------------------
        // TOTAL ACTIVE SEATS
        // --------------------------------------------------------

        var totalSpaces =
            await _context.Seats
                .AsNoTracking()
                .CountAsync(s => s.IsActive);

        // --------------------------------------------------------
        // TODAY'S BOOKED SEATS
        // --------------------------------------------------------

        var bookedCount =
            await _context.HotseatBookings
                .AsNoTracking()
                .CountAsync(b =>
                    b.BookingDate == today &&
                    (
                        b.BookingStatus == "Confirmed" ||
                        b.BookingStatus == "CheckedIn"
                    ));

        // --------------------------------------------------------
        // PENDING CHECK-IN
        // --------------------------------------------------------

        var pendingCheckInCount =
            await _context.HotseatBookings
                .AsNoTracking()
                .CountAsync(b =>
                    b.BookingDate == today &&
                    b.BookingStatus == "Confirmed" &&
                    b.CheckInTime == null &&
                    b.ReleasedOn == null);

        // --------------------------------------------------------
        // TODAY'S BOOKINGS
        // --------------------------------------------------------

        var bookingsToday =
            await _context.HotseatBookings
                .AsNoTracking()
                .CountAsync(b =>
                    b.BookingDate == today &&
                    b.BookingStatus != "Cancelled" &&
                    b.BookingStatus != "Released" &&
                    b.BookingStatus != "Expired");

        // --------------------------------------------------------
        // AVAILABLE
        // --------------------------------------------------------

        var availableCount =
            Math.Max(
                0,
                totalSpaces - bookedCount);

        return Ok(new HotseatStatsDto
        {
            TotalSpaces = totalSpaces,

            Available = availableCount,

            Booked = bookedCount,

            PendingCheckIn = pendingCheckInCount,

            BookingsToday = bookingsToday
        });
    }
}