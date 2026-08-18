using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using SpaceBook.Application.DTOs.Hotseat;

using SpaceBook.Domain.Entities;

using SpaceBook.Infrastructure.Data;

using System.Security.Claims;
 
namespace SpaceBook.API.Controllers

{

    [Route("api/[controller]")]

    [ApiController]

    public class HotseatController : ControllerBase

    {

        private readonly ApplicationDbContext _context;
 
        public HotseatController(ApplicationDbContext context)

        {

            _context = context;

        }
 
        // ============================================================

        // GET: api/Hotseat

        // Get active hotseat seats and their status for a date

        // ============================================================
 
        [HttpGet]

        public async Task<ActionResult<IEnumerable<HotseatSeatDto>>> GetOfficeMap(

            [FromQuery] string? date,

            [FromQuery] string? city,

            [FromQuery] string? building,

            [FromQuery] string? module)

        {

            // --------------------------------------------------------

            // 1. Parse requested date

            // --------------------------------------------------------
 
            DateOnly? bookingDate = null;
 
            if (!string.IsNullOrWhiteSpace(date))

            {

                if (DateOnly.TryParse(date, out var parsedDate))

                {

                    bookingDate = parsedDate;

                }

                else

                {

                    return BadRequest(new

                    {

                        message = "Invalid date format."

                    });

                }

            }
 
            // --------------------------------------------------------

            // 2. Get active seats

            // --------------------------------------------------------
 
            var seatsQuery = _context.Seats

                .AsNoTracking()

                .Where(s => s.IsActive);
 
            // --------------------------------------------------------

            // 3. Optional module filter

            // --------------------------------------------------------
 
            if (!string.IsNullOrWhiteSpace(module))

            {

                seatsQuery = seatsQuery.Where(s =>

                    s.Module != null &&

                    s.Module.ModuleName == module);

            }
 
            // --------------------------------------------------------

            // 4. Get seats and booking status

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
 
                    IsBooked = bookingDate.HasValue &&

                        s.HotseatBookings.Any(b =>

                            b.BookingDate == bookingDate.Value &&

                            (

                                b.BookingStatus == "Confirmed" ||

                                b.BookingStatus == "CheckedIn"

                            ))

                })

                .ToListAsync();
 
            // --------------------------------------------------------

            // 5. Convert to DTO

            // --------------------------------------------------------
 
            var result = seats.Select(s => new HotseatSeatDto

            {

                SeatNumber = s.SeatNumber,

                Section = s.Section,

                Row = s.Row,

                Status = s.IsBooked ? "Booked" : "Vacant"

            }).ToList();
 
            return Ok(result);

        }
 
 
        // ============================================================

        // GET: api/Hotseat/my-bookings

        // Get logged-in employee's hotseat bookings

        // ============================================================
 
        [HttpGet("my-bookings")]

        public async Task<IActionResult> GetMyBookings()

        {

            // --------------------------------------------------------

            // 1. Get employee ID from JWT

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

            // 2. Get employee bookings

            // --------------------------------------------------------
 
            var bookings = await _context.HotseatBookings

                .AsNoTracking()

                .Where(b => b.EmployeeId == employeeId)

                .Include(b => b.Seat)

                    .ThenInclude(s => s.Module)

                .OrderByDescending(b => b.BookingDate)

                .ThenByDescending(b => b.BookedOn)

                .Select(b => new

                {

                    bookingId = b.HotseatBookingId,
 
                    seatId = b.SeatId,
 
                    seatNumber = b.Seat != null

                        ? b.Seat.SeatNumber

                        : "",
 
                    module = b.Seat != null &&

                             b.Seat.Module != null

                        ? b.Seat.Module.ModuleName

                        : "",
 
                    type = "Hot Seat",
 
                    date = b.BookingDate,
 
                    expectedCheckIn = b.CheckInDeadline,
 
                    status = b.BookingStatus,
 
                    bookedOn = b.BookedOn,
 
                    checkInTime = b.CheckInTime,
 
                    releasedOn = b.ReleasedOn

                })

                .ToListAsync();
 
            return Ok(bookings);

        }
 
 
        // ============================================================

        // POST: api/Hotseat

        // Create a hotseat booking

        // ============================================================
 
        [HttpPost]

        public async Task<IActionResult> CreateBooking(

            [FromBody] CreateHotseatBookingDto request)

        {

            // --------------------------------------------------------

            // 1. Validate request

            // --------------------------------------------------------
 
            if (request == null)

            {

                return BadRequest(new

                {

                    message = "Booking request is required."

                });

            }
 
            // --------------------------------------------------------

            // 2. Validate booking date

            // --------------------------------------------------------
 
            var todayUtc =

                DateOnly.FromDateTime(DateTime.UtcNow);
 
            if (request.BookingDate < todayUtc)

            {

                return BadRequest(new

                {

                    message = "Booking date cannot be in the past."

                });

            }
 
            // --------------------------------------------------------

            // 3. Validate seat

            // --------------------------------------------------------
 
            var seat = await _context.Seats

                .Include(s => s.Module)

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

            // 4. Get employee ID from JWT

            // --------------------------------------------------------
 
            var employeeIdClaim =

                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
 
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

                    message = "Invalid employee ID."

                });

            }
 
            // --------------------------------------------------------

            // 5. Check employee exists

            // --------------------------------------------------------
 
            var employeeExists = await _context.Employees

                .AnyAsync(e =>

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

            // 6. Check seat already booked

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

            // 7. Prevent employee duplicate booking

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

            // 8. Create UTC booked time

            // --------------------------------------------------------
 
            var bookedOnUtc = DateTime.UtcNow;
 
            // --------------------------------------------------------

            // 9. Create check-in deadline

            // --------------------------------------------------------
 
            var checkInTime =

                request.ExpectedCheckInTime ??

                new TimeOnly(9, 0, 0);
 
            var checkInDeadlineUtc =

                DateTime.SpecifyKind(

                    request.BookingDate.ToDateTime(checkInTime),

                    DateTimeKind.Utc);
 
            // --------------------------------------------------------

            // 10. Create booking

            // --------------------------------------------------------
 
            var booking = new HotseatBooking

            {

                SeatId = request.SeatId,
 
                EmployeeId = employeeId,
 
                BookingDate = request.BookingDate,
 
                BookingStatus = "Confirmed",
 
                BookedOn = bookedOnUtc,
 
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
 
            // --------------------------------------------------------

            // 11. Save

            // --------------------------------------------------------
 
            try

            {

                _context.HotseatBookings.Add(booking);
 
                await _context.SaveChangesAsync();

            }

            catch (DbUpdateException ex)

            {

                Console.WriteLine(

                    "HOTSEAT BOOKING DATABASE ERROR:");
 
                Console.WriteLine(ex.ToString());
 
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

            // 12. Return response

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

        // Edit a hotseat booking

        // ============================================================
 
        [HttpPut("{id:int}")]

        public async Task<IActionResult> UpdateBooking(

            int id,

            [FromBody] CreateHotseatBookingDto request)

        {

            // --------------------------------------------------------

            // 1. Validate request

            // --------------------------------------------------------
 
            if (request == null)

            {

                return BadRequest(new

                {

                    message = "Booking request is required."

                });

            }
 
            // --------------------------------------------------------

            // 2. Get employee ID

            // --------------------------------------------------------
 
            var employeeIdClaim =

                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
 
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

            // 3. Find booking

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
 
            // --------------------------------------------------------

            // 4. Verify ownership

            // --------------------------------------------------------
 
            if (booking.EmployeeId != employeeId)

            {

                return Forbid();

            }
 
            // --------------------------------------------------------

            // 5. Check booking status

            // --------------------------------------------------------
 
            if (booking.BookingStatus != "Confirmed")

            {

                return BadRequest(new

                {

                    message =

                        "Only confirmed bookings can be edited."

                });

            }
 
            // --------------------------------------------------------

            // 6. Validate booking date

            // --------------------------------------------------------
 
            var todayUtc =

                DateOnly.FromDateTime(DateTime.UtcNow);
 
            if (request.BookingDate < todayUtc)

            {

                return BadRequest(new

                {

                    message =

                        "Booking date cannot be in the past."

                });

            }
 
            // --------------------------------------------------------

            // 7. Validate new seat

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

            // 8. Check seat availability

            // Exclude current booking

            // --------------------------------------------------------
 
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
 
            // --------------------------------------------------------

            // 9. Check employee already has another booking

            // --------------------------------------------------------
 
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
            // --------------------------------------------------------

            // 10. Calculate UTC check-in deadline

            // --------------------------------------------------------
 
            var checkInTime =

                request.ExpectedCheckInTime ??

                new TimeOnly(9, 0, 0);
 
            var checkInDeadlineUtc =

                DateTime.SpecifyKind(

                    request.BookingDate.ToDateTime(checkInTime),

                    DateTimeKind.Utc);
 
            // --------------------------------------------------------

            // 11. Update booking

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
 
            // --------------------------------------------------------

            // 12. Save

            // --------------------------------------------------------
 
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
 
            // --------------------------------------------------------

            // 13. Return

            // --------------------------------------------------------
 
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

        // Check in to a hotseat booking

        // ============================================================
 
        [HttpPost("{id:int}/check-in")]

        public async Task<IActionResult> CheckIn(int id)

        {

            // --------------------------------------------------------

            // 1. Get employee ID

            // --------------------------------------------------------
 
            var employeeIdClaim =

                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
 
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

            // 2. Find booking

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
 
            // --------------------------------------------------------

            // 3. Verify ownership

            // --------------------------------------------------------
 
            if (booking.EmployeeId != employeeId)

            {

                return Forbid();

            }
 
            // --------------------------------------------------------

            // 4. Check status

            // --------------------------------------------------------
 
            if (booking.BookingStatus == "CheckedIn")

            {

                return BadRequest(new

                {

                    message =

                        "You have already checked in to this hotseat."

                });

            }
 
            if (booking.BookingStatus == "Cancelled")

            {

                return BadRequest(new

                {

                    message =

                        "Cancelled bookings cannot be checked in."

                });

            }
 
            if (booking.BookingStatus == "Released")

            {

                return BadRequest(new

                {

                    message =

                        "Released bookings cannot be checked in."

                });

            }
 
            if (booking.BookingStatus == "Expired")

            {

                return BadRequest(new

                {

                    message =

                        "Expired bookings cannot be checked in."

                });

            }
 
            if (booking.BookingStatus != "Confirmed")

            {

                return BadRequest(new

                {

                    message =

                        "This booking cannot be checked in."

                });

            }
 
            // --------------------------------------------------------

            // 5. Validate check-in date

            // --------------------------------------------------------
 
            var todayUtc =

                DateOnly.FromDateTime(DateTime.UtcNow);
 
            if (booking.BookingDate != todayUtc)

            {

                return BadRequest(new

                {

                    message =

                        "You can only check in on the booking date."

                });

            }
 
            // --------------------------------------------------------

            // 6. Set check-in information

            // --------------------------------------------------------
 
            var checkInTimeUtc =

                DateTime.UtcNow;
 
            booking.BookingStatus =

                "CheckedIn";
 
            booking.CheckInTime =

                checkInTimeUtc;
 
            booking.RecordModifiedBy =

                employeeId.ToString();
 
            booking.RecordModifiedOn =

                DateTime.UtcNow;
 
            // --------------------------------------------------------

            // 7. Save

            // --------------------------------------------------------
 
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
 
            // --------------------------------------------------------

            // 8. Return

            // --------------------------------------------------------
 
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

        // Cancel a hotseat booking

        //

        // IMPORTANT:

        // This performs a SOFT DELETE.

        // The database row is NOT physically deleted.

        // ============================================================
 
        [HttpDelete("{id:int}")]

        public async Task<IActionResult> CancelBooking(int id)

        {

            // --------------------------------------------------------

            // 1. Get logged-in employee

            // --------------------------------------------------------
 
            var employeeIdClaim =

                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
 
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

            // 2. Find booking

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
 
            // --------------------------------------------------------

            // 3. Make sure employee owns booking

            // --------------------------------------------------------
 
            if (booking.EmployeeId != employeeId)

            {

                return Forbid();

            }
 
            // --------------------------------------------------------

            // 4. Check current status

            // --------------------------------------------------------
 
            if (booking.BookingStatus == "Cancelled")

            {

                return BadRequest(new

                {

                    message =

                        "This hotseat booking is already cancelled."

                });

            }
 
            if (booking.BookingStatus == "Released")

            {

                return BadRequest(new

                {

                    message =

                        "This hotseat booking has already been released."

                });

            }
 
            if (booking.BookingStatus == "Expired")

            {

                return BadRequest(new

                {

                    message =

                        "This hotseat booking has expired."

                });

            }
 
            // --------------------------------------------------------

            // 5. Cancel instead of deleting row

            // --------------------------------------------------------
 
            booking.BookingStatus =

                "Cancelled";
 
            // --------------------------------------------------------

            // 6. Audit information

            // --------------------------------------------------------
 
            booking.RecordModifiedBy =

                employeeId.ToString();
 
            booking.RecordModifiedOn =

                DateTime.UtcNow;
 
            // --------------------------------------------------------

            // 7. Save

            // --------------------------------------------------------
 
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
 
            // --------------------------------------------------------

            // 8. Return

            // --------------------------------------------------------
 
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

}
 
 