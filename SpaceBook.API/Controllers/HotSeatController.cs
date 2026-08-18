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

        // Get all active hotseat seats

        // ============================================================
 
        [HttpGet]

        public async Task<ActionResult<IEnumerable<HotseatSeatDto>>> GetOfficeMap(

            [FromQuery] string? date,

            [FromQuery] string? city,

            [FromQuery] string? building,

            [FromQuery] string? module)

        {

            // --------------------------------------------------------

            // Parse requested date

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

            // Get active seats

            // --------------------------------------------------------
 
            var seatsQuery = _context.Seats

                .AsNoTracking()

                .Where(s => s.IsActive);
 
            // --------------------------------------------------------

            // Optional module filter

            // --------------------------------------------------------
 
            if (!string.IsNullOrWhiteSpace(module))

            {

                seatsQuery = seatsQuery.Where(s =>

                    s.Module != null &&

                    s.Module.ModuleName == module);

            }
 
            // --------------------------------------------------------

            // Get seats and determine status

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

            // Convert to DTO

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
 
            var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
 
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

                    message = "Employee information could not be determined."

                });

            }
 
            if (!int.TryParse(employeeIdClaim, out int employeeId))

            {

                return Unauthorized(new

                {

                    message = "Invalid employee ID."

                });

            }
 
            // --------------------------------------------------------

            // 5. Check if employee exists

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

            // 6. Check if seat is already booked

            //

            // Only Confirmed and CheckedIn bookings block the seat.

            // Cancelled, Released and Expired bookings do not.

            // --------------------------------------------------------
 
            var existingBooking = await _context.HotseatBookings

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

                    existingBookingId = existingBooking.HotseatBookingId,

                    bookingStatus = existingBooking.BookingStatus

                });

            }
 
            // --------------------------------------------------------

            // 7. Prevent employee from making duplicate booking

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

            // 8. Create UTC BookedOn

            // --------------------------------------------------------
 
            var bookedOnUtc = DateTime.UtcNow;
 
            // --------------------------------------------------------

            // 9. Create Check-In Deadline

            //

            // If expectedCheckInTime is supplied:

            //

            // 2026-08-19 + 09:00:00

            //

            // becomes:

            //

            // 2026-08-19 09:00:00 UTC

            //

            // IMPORTANT:

            // DateOnly.ToDateTime() creates an Unspecified DateTime.

            // We explicitly specify UTC before saving to PostgreSQL.

            // --------------------------------------------------------
 
            var checkInTime =

                request.ExpectedCheckInTime

                ?? new TimeOnly(9, 0, 0);
 
            var checkInDeadlineUtc = DateTime.SpecifyKind(

                request.BookingDate.ToDateTime(checkInTime),

                DateTimeKind.Utc

            );
 
            // --------------------------------------------------------

            // 10. Create booking entity

            // --------------------------------------------------------
 
            var booking = new HotseatBooking

            {

                SeatId = request.SeatId,
 
                EmployeeId = employeeId,
 
                BookingDate = request.BookingDate,
 
                BookingStatus = "Confirmed",
 
                // UTC

                BookedOn = bookedOnUtc,
 
                // UTC

                CheckInDeadline = checkInDeadlineUtc,
 
                // These are NULL initially

                CheckInTime = null,
 
                ReleasedOn = null,
 
                RecordIngestedBy = employeeId.ToString(),
 
                // UTC

                RecordIngestedOn = DateTime.UtcNow,
 
                RecordModifiedBy = null,
 
                RecordModifiedOn = null

            };
 
            // --------------------------------------------------------

            // 11. Save booking

            // --------------------------------------------------------
 
            try

            {

                _context.HotseatBookings.Add(booking);
 
                await _context.SaveChangesAsync();

            }

            catch (DbUpdateException ex)

            {

                // Log the real database error

                Console.WriteLine(

                    "HOTSEAT BOOKING DATABASE ERROR:"

                );
 
                Console.WriteLine(ex.ToString());
 
                return StatusCode(500, new

                {

                    message =

                        "An error occurred while saving the hotseat booking.",

                    detail =

                        ex.InnerException?.Message ?? ex.Message

                });

            }
 
            // --------------------------------------------------------

            // 12. Return successful response

            // --------------------------------------------------------
 
            return Ok(new

            {

                message = "Hotseat booked successfully.",
 
                bookingId = booking.HotseatBookingId,
 
                seatId = booking.SeatId,
 
                seatNumber = seat.SeatNumber,
 
                employeeId = booking.EmployeeId,
 
                bookingDate = booking.BookingDate,
 
                bookingStatus = booking.BookingStatus,
 
                bookedOn = booking.BookedOn,
 
                checkInDeadline = booking.CheckInDeadline

            });

        }

    }

}
 