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
    private readonly IEmailService _emailService;
    private readonly IServiceScopeFactory _scopeFactory;

    private static readonly TimeZoneInfo IndiaTimeZone = GetIndiaTimeZone();

    private static TimeZoneInfo GetIndiaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
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
        INotificationRepository notificationRepository,
        IEmailService emailService,
        IServiceScopeFactory scopeFactory)
    {
        _context = context;
        _notificationRepository = notificationRepository;
        _emailService = emailService;
        _scopeFactory = scopeFactory;
    }

    private async Task AutoExpireOverdueHotseatBookingsAsync()
    {
        try
        {
            var nowUtc = DateTime.UtcNow;
            var today = GetIndiaToday();

            var overdueBookings = await _context.HotseatBookings
                .Where(b => b.BookingStatus == "Confirmed" &&
                            b.CheckInTime == null &&
                            (b.BookingDate < today ||
                             (b.BookingDate == today && b.CheckInDeadline.HasValue && b.CheckInDeadline.Value < nowUtc)))
                .ToListAsync();

            if (overdueBookings.Any())
            {
                foreach (var b in overdueBookings)
                {
                    b.BookingStatus = "Expired";
                    b.RecordModifiedBy = "System (Auto-Expired)";
                    b.RecordModifiedOn = nowUtc;
                }

                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HotseatController] AutoExpire error: {ex.Message}");
        }
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
        await AutoExpireOverdueHotseatBookingsAsync();

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
        await AutoExpireOverdueHotseatBookingsAsync();

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

        var rawBookings = await _context.HotseatBookings
            .AsNoTracking()
            .Where(b => b.EmployeeId == employeeId)
            .Include(b => b.Seat)
                .ThenInclude(s => s!.Module)
                    .ThenInclude(m => m!.Office)
                        .ThenInclude(o => o!.Location)
            .OrderByDescending(b => b.BookingDate)
            .ThenByDescending(b => b.BookedOn)
            .ToListAsync();

        var bookings = rawBookings.Select(b =>
        {
            DateTime? localDeadline = b.CheckInDeadline.HasValue
                ? TimeZoneInfo.ConvertTimeFromUtc(b.CheckInDeadline.Value, IndiaTimeZone)
                : null;

            DateTime? localStartTime = localDeadline.HasValue
                ? localDeadline.Value.AddHours(-1)
                : null;

            TimeOnly? expectedTimeOnly = localStartTime.HasValue
                ? TimeOnly.FromDateTime(localStartTime.Value)
                : null;

            DateTime? localBookedOn = TimeZoneInfo.ConvertTimeFromUtc(b.BookedOn, IndiaTimeZone);
            DateTime? localCheckInTime = b.CheckInTime.HasValue
                ? TimeZoneInfo.ConvertTimeFromUtc(b.CheckInTime.Value, IndiaTimeZone)
                : null;

            return new
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

                expectedCheckInTime = expectedTimeOnly.HasValue ? expectedTimeOnly.Value.ToString("HH:mm") : null,
                expectedCheckInTimeFormatted = expectedTimeOnly.HasValue ? expectedTimeOnly.Value.ToString("hh:mm tt") : null,

                expectedCheckIn = localStartTime.HasValue ? localStartTime.Value.ToString("yyyy-MM-ddTHH:mm:ss") : null,
                checkInDeadline = localDeadline.HasValue ? localDeadline.Value.ToString("yyyy-MM-ddTHH:mm:ss") : null,
                expectedCheckInLocal = localStartTime,
                checkInDeadlineLocal = localDeadline,

                status = b.BookingStatus,
                bookingStatus = b.BookingStatus,

                bookedOn = localBookedOn.HasValue ? localBookedOn.Value.ToString("yyyy-MM-ddTHH:mm:ss") : null,
                bookedTime = localBookedOn.HasValue ? localBookedOn.Value.ToString("yyyy-MM-ddTHH:mm:ss") : null,
                bookedOnLocal = localBookedOn,

                checkInTime = localCheckInTime.HasValue ? localCheckInTime.Value.ToString("yyyy-MM-ddTHH:mm:ss") : null,
                checkInTimeLocal = localCheckInTime,

                releasedOn = b.ReleasedOn
            };
        }).ToList();

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
        await AutoExpireOverdueHotseatBookingsAsync();

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
        // CHECK-IN DEADLINE (1 hour after expected check-in / start time)
        // --------------------------------------------------------

        var expectedCheckInTime =
            request.ExpectedCheckInTime ??
            new TimeOnly(9, 0, 0);

        var localStartTime =
            request.BookingDate.ToDateTime(expectedCheckInTime);

        var localCheckInDeadline =
            localStartTime.AddHours(1);

        var checkInDeadlineUtc =
            TimeZoneInfo.ConvertTimeToUtc(
                localCheckInDeadline,
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
        // NOTIFICATIONS & EMAIL
        // --------------------------------------------------------

        var startTimeStr = expectedCheckInTime.ToString("hh:mm tt");
        await CreateHotseatNotificationAsync(
            employeeId,
            booking.HotseatBookingId,
            $"Your hotseat booking for {seat.SeatNumber} in {seat.Module?.ModuleName} on {booking.BookingDate:dd-MMM-yyyy} starting at {startTimeStr} has been confirmed. Booking ID: #{booking.HotseatBookingId}.");

        var createdBookingId = booking.HotseatBookingId;
        var createdSeatId = booking.SeatId;

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var employee = await context.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

                var seatDetails = await context.Seats
                    .Include(s => s.Module)
                        .ThenInclude(m => m!.Office)
                            .ThenInclude(o => o!.Location)
                    .FirstOrDefaultAsync(s => s.SeatId == createdSeatId);

                var bookingEntity = await context.HotseatBookings
                    .FirstOrDefaultAsync(b => b.HotseatBookingId == createdBookingId);

                var adminEmails = await context.Employees
                    .AsNoTracking()
                    .Include(e => e.Role)
                    .Where(e => e.Role != null &&
                                (e.Role.RoleName == "Admin" || e.Role.RoleName == "ADMIN" || e.Role.RoleName == "admin") &&
                                !string.IsNullOrWhiteSpace(e.Email))
                    .Select(e => e.Email)
                    .ToListAsync();

                if (employee != null && !string.IsNullOrWhiteSpace(employee.Email) && bookingEntity != null)
                {
                    await emailService.SendHotseatBookingConfirmationAsync(
                        bookingEntity,
                        employee,
                        seatDetails ?? new Seat { SeatId = createdSeatId, SeatNumber = $"Seat {createdSeatId}" },
                        adminEmails);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HotseatController] Background confirmation email failed: {ex.Message}");
            }
        });

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

            expectedCheckInTime =
                expectedCheckInTime.ToString("HH:mm"),

            expectedCheckInTimeFormatted =
                expectedCheckInTime.ToString("hh:mm tt"),

            expectedCheckIn =
                localStartTime.ToString("yyyy-MM-ddTHH:mm:ss"),

            expectedCheckInLocal =
                localStartTime,

            checkInDeadline =
                localCheckInDeadline.ToString("yyyy-MM-ddTHH:mm:ss"),

            checkInDeadlineLocal =
                localCheckInDeadline,

            bookingStatus =
                booking.BookingStatus,

            bookedOn =
                GetIndiaNow().ToString("yyyy-MM-ddTHH:mm:ss")
        });
    }

    // ============================================================
    // PUT: api/Hotseat/{id}
    // UPDATE HOTSEAT BOOKING (EDIT SEAT, DATE, OR CHECK-IN TIME)
    // ============================================================

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateBooking(
        int id,
        [FromBody] UpdateHotseatBookingDto request)
    {
        await AutoExpireOverdueHotseatBookingsAsync();

        if (request == null)
        {
            return BadRequest(new
            {
                message = "Booking update request is required."
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
                .Include(b => b.Seat)
                    .ThenInclude(s => s!.Module)
                        .ThenInclude(m => m!.Office)
                            .ThenInclude(o => o!.Location)
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
                    $"Only confirmed bookings can be edited. Current status is '{booking.BookingStatus}'."
            });
        }

        // --------------------------------------------------------
        // CHECK-IN DEADLINE VALIDATION
        // Check-in time / booking can only be edited BEFORE the deadline
        // --------------------------------------------------------

        if (booking.CheckInDeadline.HasValue && DateTime.UtcNow > booking.CheckInDeadline.Value)
        {
            return BadRequest(new
            {
                message = "Check-in time cannot be edited after the check-in deadline has passed."
            });
        }

        // --------------------------------------------------------
        // TARGET BOOKING DATE
        // --------------------------------------------------------

        var targetBookingDate = request.BookingDate.HasValue && request.BookingDate.Value != default
            ? request.BookingDate.Value
            : booking.BookingDate;

        var today = GetIndiaToday();

        if (targetBookingDate < today)
        {
            return BadRequest(new
            {
                message = "Booking date cannot be in the past."
            });
        }

        // --------------------------------------------------------
        // TARGET SEAT RESOLUTION
        // --------------------------------------------------------

        int targetSeatId = request.SeatId.HasValue && request.SeatId.Value > 0
            ? request.SeatId.Value
            : booking.SeatId;

        if (!string.IsNullOrWhiteSpace(request.SeatNumber) && (!request.SeatId.HasValue || request.SeatId.Value <= 0))
        {
            var seatQuery = _context.Seats
                .Include(s => s.Module)
                .Where(s =>
                    s.IsActive &&
                    s.SeatNumber.ToLower() == request.SeatNumber.Trim().ToLower());

            var moduleFilter = !string.IsNullOrWhiteSpace(request.ModuleName)
                ? request.ModuleName
                : request.Module;

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
                targetSeatId = matchedSeat.SeatId;
            }
        }

        var seat = await _context.Seats
            .Include(s => s.Module)
                .ThenInclude(m => m!.Office)
                    .ThenInclude(o => o!.Location)
            .FirstOrDefaultAsync(s =>
                s.SeatId == targetSeatId &&
                s.IsActive);

        if (seat == null)
        {
            return NotFound(new
            {
                message = "Seat not found or inactive."
            });
        }

        // --------------------------------------------------------
        // PREVENT SEAT OVERLAP (IF SEAT OR DATE CHANGED)
        // --------------------------------------------------------

        var seatAlreadyBooked =
            await _context.HotseatBookings
                .AsNoTracking()
                .AnyAsync(b =>
                    b.HotseatBookingId != id &&
                    b.SeatId == targetSeatId &&
                    b.BookingDate == targetBookingDate &&
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

        // --------------------------------------------------------
        // PREVENT EMPLOYEE DUPLICATE (IF DATE CHANGED)
        // --------------------------------------------------------

        var employeeAlreadyBooked =
            await _context.HotseatBookings
                .AsNoTracking()
                .AnyAsync(b =>
                    b.HotseatBookingId != id &&
                    b.EmployeeId == employeeId &&
                    b.BookingDate == targetBookingDate &&
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

        // --------------------------------------------------------
        // CALCULATE NEW CHECK-IN DEADLINE
        // --------------------------------------------------------

        TimeOnly newCheckInTime;

        if (request.ExpectedCheckInTime.HasValue)
        {
            newCheckInTime = request.ExpectedCheckInTime.Value;
        }
        else if (booking.CheckInDeadline.HasValue)
        {
            var existingLocalDeadline = TimeZoneInfo.ConvertTimeFromUtc(
                booking.CheckInDeadline.Value,
                IndiaTimeZone);
            newCheckInTime = TimeOnly.FromDateTime(existingLocalDeadline.AddHours(-1));
        }
        else
        {
            newCheckInTime = new TimeOnly(9, 0, 0);
        }

        var localStartTime = targetBookingDate.ToDateTime(newCheckInTime);
        var localCheckInDeadline = localStartTime.AddHours(1);

        var checkInDeadlineUtc = TimeZoneInfo.ConvertTimeToUtc(
            localCheckInDeadline,
            IndiaTimeZone);

        // If updating for today, check-in deadline (1 hr after start) must not be in the past
        if (targetBookingDate == today && checkInDeadlineUtc < DateTime.UtcNow)
        {
            return BadRequest(new
            {
                message = "The check-in deadline for this time slot has already passed."
            });
        }

        // --------------------------------------------------------
        // SAVE CHANGES
        // --------------------------------------------------------

        booking.SeatId = targetSeatId;
        booking.BookingDate = targetBookingDate;
        booking.CheckInDeadline = checkInDeadlineUtc;
        booking.RecordModifiedBy = employeeId.ToString();
        booking.RecordModifiedOn = DateTime.UtcNow;

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
            $"Your hotseat booking has been updated for {seat.SeatNumber} " +
            $"on {booking.BookingDate:dd-MMM-yyyy} (Check-in time: {newCheckInTime:HH:mm}).");

        var updatedBookingId = booking.HotseatBookingId;
        var updatedSeatId = booking.SeatId;

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var employee = await context.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

                var seatDetails = await context.Seats
                    .Include(s => s.Module)
                        .ThenInclude(m => m!.Office)
                            .ThenInclude(o => o!.Location)
                    .FirstOrDefaultAsync(s => s.SeatId == updatedSeatId);

                var bookingEntity = await context.HotseatBookings
                    .FirstOrDefaultAsync(b => b.HotseatBookingId == updatedBookingId);

                var adminEmails = await context.Employees
                    .AsNoTracking()
                    .Include(e => e.Role)
                    .Where(e => e.Role != null &&
                                (e.Role.RoleName == "Admin" || e.Role.RoleName == "ADMIN" || e.Role.RoleName == "admin") &&
                                !string.IsNullOrWhiteSpace(e.Email))
                    .Select(e => e.Email)
                    .ToListAsync();

                if (employee != null && !string.IsNullOrWhiteSpace(employee.Email) && bookingEntity != null)
                {
                    await emailService.SendHotseatBookingRescheduledAsync(
                        bookingEntity,
                        employee,
                        seatDetails ?? new Seat { SeatId = updatedSeatId, SeatNumber = $"Seat {updatedSeatId}" },
                        adminEmails);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HotseatController] Background reschedule email failed: {ex.Message}");
            }
        });

        return Ok(new
        {
            message = "Hotseat booking updated successfully.",
            bookingId = booking.HotseatBookingId,
            seatId = booking.SeatId,
            seatNumber = seat.SeatNumber,
            module = seat.Module?.ModuleName,
            building = seat.Module?.Office?.OfficeName,
            city = seat.Module?.Office?.Location?.LocationName,
            bookingDate = booking.BookingDate,
            expectedCheckInTime = newCheckInTime.ToString("HH:mm"),
            expectedCheckInTimeFormatted = newCheckInTime.ToString("hh:mm tt"),
            expectedCheckIn = localStartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
            expectedCheckInLocal = localStartTime,
            checkInDeadline = localCheckInDeadline.ToString("yyyy-MM-ddTHH:mm:ss"),
            checkInDeadlineLocal = localCheckInDeadline,
            bookingStatus = booking.BookingStatus,
            modifiedOn = GetIndiaNow().ToString("yyyy-MM-ddTHH:mm:ss")
        });
    }

    // ============================================================
    // POST: api/Hotseat/{id}/check-in
    // ============================================================

    [HttpPost("{id:int}/check-in")]
    public async Task<IActionResult> CheckIn(int id)
    {
        await AutoExpireOverdueHotseatBookingsAsync();

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
        var nowIst = GetIndiaNow();

        if (booking.BookingDate < today)
        {
            booking.BookingStatus = "Expired";
            booking.RecordModifiedBy = "System (Auto-Expired)";
            booking.RecordModifiedOn = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return BadRequest(new
            {
                message = "Check-in window has expired. Your booking has been marked as Expired and the seat released."
            });
        }

        if (booking.BookingDate > today)
        {
            return BadRequest(new
            {
                message =
                    "You can only check in on the booking date."
            });
        }

        // Booking check-in deadline in IST (1 hour after booking start time)
        DateTime checkInDeadlineIst = booking.CheckInDeadline.HasValue
            ? TimeZoneInfo.ConvertTimeFromUtc(booking.CheckInDeadline.Value, IndiaTimeZone)
            : booking.BookingDate.ToDateTime(new TimeOnly(10, 0, 0));

        DateTime bookingStartIst = checkInDeadlineIst.AddHours(-1);
        DateTime checkInWindowStart = bookingStartIst.AddHours(-1);

        // Requirement 2: Reject if employee tries to check in before the check-in window opens (1 hr before start)
        if (nowIst < checkInWindowStart)
        {
            return BadRequest(new
            {
                message = $"Check-in is available only from {checkInWindowStart:hh:mm tt} (1 hour before the booking start time)."
            });
        }

        // Requirement 3: Reject and Auto-Expire if employee missed the check-in deadline (1 hr after start)
        if (nowIst > checkInDeadlineIst)
        {
            booking.BookingStatus = "Expired";
            booking.RecordModifiedBy = "System (Auto-Expired)";
            booking.RecordModifiedOn = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var seatObj = booking.Seat ?? new Seat { SeatId = booking.SeatId, SeatNumber = $"Seat {booking.SeatId}" };
            var empObj = await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (empObj != null && !string.IsNullOrWhiteSpace(empObj.Email))
            {
                try
                {
                    await _emailService.SendHotseatBookingExpiredAsync(booking, empObj, seatObj);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HotseatController] Expiration email failed: {ex.Message}");
                }
            }

            var sNum = seatObj.SeatNumber;
            var modName = seatObj.Module?.ModuleName ?? "Module";
            await CreateHotseatNotificationAsync(
                employeeId,
                booking.HotseatBookingId,
                $"Hotseat Booking Expired: You did not check in within the permitted time for {sNum} in {modName}. Your reservation has expired and the seat has been released.");

            return BadRequest(new
            {
                message = "Check-in window has expired. Your booking has been marked as Expired and the seat released."
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
                GetIndiaNow().ToString("yyyy-MM-ddTHH:mm:ss")
        });
    }

    // ============================================================
    // POST: api/Hotseat/{id}/release
    // RELEASE HOTSEAT (CHECK-OUT / RELEASE SEAT)
    // ============================================================

    [HttpPost("{id:int}/release")]
    public async Task<IActionResult> ReleaseSeat(int id)
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
                "Released",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "This hotseat booking is already released."
            });
        }

        if (string.Equals(
                booking.BookingStatus,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "Cancelled bookings cannot be released."
            });
        }

        if (string.Equals(
                booking.BookingStatus,
                "Expired",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "Expired bookings cannot be released."
            });
        }

        booking.BookingStatus = "Released";
        booking.ReleasedOn = DateTime.UtcNow;
        booking.RecordModifiedBy = employeeId.ToString();
        booking.RecordModifiedOn = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            return StatusCode(500, new
            {
                message = "An error occurred while releasing the hotseat booking.",
                detail = ex.InnerException?.Message ?? ex.Message
            });
        }

        var seatNumber =
            booking.Seat?.SeatNumber ??
            $"Seat {booking.SeatId}";

        await CreateHotseatNotificationAsync(
            employeeId,
            booking.HotseatBookingId,
            $"Your hotseat booking for {seatNumber} has been released successfully.");

        return Ok(new
        {
            message = "Hotseat booking released successfully.",
            bookingId = booking.HotseatBookingId,
            seatId = booking.SeatId,
            employeeId = booking.EmployeeId,
            bookingDate = booking.BookingDate,
            bookingStatus = booking.BookingStatus,
            releasedOn = GetIndiaNow().ToString("yyyy-MM-ddTHH:mm:ss")
        });
    }

    // ============================================================
    // DELETE: api/Hotseat/{id}
    // CANCEL HOTSEAT BOOKING
    // ============================================================

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        await AutoExpireOverdueHotseatBookingsAsync();

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
            $"Your hotseat booking for {seatNumber} on {booking.BookingDate:dd-MMM-yyyy} has been cancelled. Reason: You cancelled the booking.");

        var cancelBookingId = booking.HotseatBookingId;
        var cancelSeatId = booking.SeatId;
        var cancelSeatNumber = seatNumber;

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var empObj = await context.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

                var seatObj = await context.Seats
                    .Include(s => s.Module)
                        .ThenInclude(m => m!.Office)
                            .ThenInclude(o => o!.Location)
                    .FirstOrDefaultAsync(s => s.SeatId == cancelSeatId);

                var bookingObj = await context.HotseatBookings
                    .FirstOrDefaultAsync(b => b.HotseatBookingId == cancelBookingId);

                var adminEmails = await context.Employees
                    .AsNoTracking()
                    .Include(e => e.Role)
                    .Where(e => e.Role != null &&
                                (e.Role.RoleName == "Admin" || e.Role.RoleName == "ADMIN" || e.Role.RoleName == "admin") &&
                                !string.IsNullOrWhiteSpace(e.Email))
                    .Select(e => e.Email)
                    .ToListAsync();

                if (empObj != null && !string.IsNullOrWhiteSpace(empObj.Email) && bookingObj != null)
                {
                    await emailService.SendHotseatBookingCancelledAsync(
                        bookingObj,
                        empObj,
                        seatObj ?? new Seat { SeatId = cancelSeatId, SeatNumber = cancelSeatNumber },
                        adminEmails,
                        "Cancelled by user");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HotseatController] Background cancellation email failed: {ex.Message}");
            }
        });

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
                GetIndiaNow().ToString("yyyy-MM-ddTHH:mm:ss")
        });
    }

    // ============================================================
    // GET: api/Hotseat/stats
    // ============================================================

    [HttpGet("stats")]
    public async Task<ActionResult<HotseatStatsDto>> GetStats()
    {
        await AutoExpireOverdueHotseatBookingsAsync();

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