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
            [FromQuery] string date,
            [FromQuery] string city,
            [FromQuery] string building,
            [FromQuery] string module)
        {
            var seats = await _context.Seats
                .Where(s => s.IsActive)
                .OrderBy(s => s.ModuleId)
                .ThenBy(s => s.Section)
                .ThenBy(s => s.RowNumber)
                .ThenBy(s => s.ColumnNumber)
                .Select(s => new HotseatSeatDto
                {
                    SeatNumber = s.SeatNumber,
                    Section = s.Section ?? "",
                    Row = s.RowNumber,
 
                    // Currently returning Vacant.
                    // Later this can be calculated from HotseatBooking.
                    Status = "Vacant"
                })
                .ToListAsync();
 
            return Ok(seats);
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
            // 2. Validate seat
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
            // 3. Get employee ID from logged-in user
            // --------------------------------------------------------
            var employeeIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
 
            if (string.IsNullOrEmpty(employeeIdClaim))
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
            // 4. Check whether the seat is already booked
            // --------------------------------------------------------
            var existingBooking = await _context.HotseatBookings
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
                    message = "This seat is already booked for the selected date."
                });
            }
 
 
            // --------------------------------------------------------
            // 5. Prevent employee from making duplicate booking
            // --------------------------------------------------------
            var employeeExistingBooking =
                await _context.HotseatBookings
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
                    message = "You already have a hotseat booking for this date."
                });
            }
 
 
            // --------------------------------------------------------
            // 6. Validate booking date
            // --------------------------------------------------------
            var todayUtc = DateTime.UtcNow.Date;
 
            if (request.BookingDate < DateOnly.FromDateTime(todayUtc))
            {
                return BadRequest(new
                {
                    message = "Booking date cannot be in the past."
                });
            }
 
 
            // --------------------------------------------------------
            // 7. Create UTC BookedOn
            // --------------------------------------------------------
            DateTime bookedOnUtc = DateTime.UtcNow;
 
 
            // --------------------------------------------------------
            // 8. Create Check-In Deadline
            //
            // Example:
            // bookingDate = 2026-08-19
            //
            // If your business rule says the employee must check in
            // by 09:00, create the DateTime in UTC.
            // --------------------------------------------------------
 
            DateTime checkInDeadlineUtc;
 
            if (request.ExpectedCheckInTime.HasValue)
            {
                checkInDeadlineUtc = new DateTime(
                    request.BookingDate.Year,
                    request.BookingDate.Month,
                    request.BookingDate.Day,
                    request.ExpectedCheckInTime.Value.Hour,
                    request.ExpectedCheckInTime.Value.Minute,
                    request.ExpectedCheckInTime.Value.Second,
                    DateTimeKind.Utc);
            }
            else
            {
                // Default check-in deadline: 09:00 UTC
                checkInDeadlineUtc = new DateTime(
                    request.BookingDate.Year,
                    request.BookingDate.Month,
                    request.BookingDate.Day,
                    9,
                    0,
                    0,
                    DateTimeKind.Utc);
            }
 
 
            // --------------------------------------------------------
            // 9. Create booking entity
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
 
                RecordIngestedBy = employeeId.ToString(),
 
                RecordIngestedOn = DateTime.UtcNow,
 
                RecordModifiedBy = null,
 
                RecordModifiedOn = null
            };
 
 
            // --------------------------------------------------------
            // 10. Save booking
            // --------------------------------------------------------
            _context.HotseatBookings.Add(booking);
 
            await _context.SaveChangesAsync();
 
 
            // --------------------------------------------------------
            // 11. Return successful response
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