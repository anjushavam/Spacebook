using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Hotseat;
using SpaceBook.Domain.Entities;
using SpaceBook.Infrastructure.Data;
 
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
        // ============================================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<HotseatSeatDto>>> GetOfficeMap(
            [FromQuery] string date,
            [FromQuery] string city,
            [FromQuery] string building,
            [FromQuery] string module)
        {
            // --------------------------------------------------------
            // 1. Validate date
            // --------------------------------------------------------
 
            if (!DateOnly.TryParse(date, out var bookingDate))
            {
                return BadRequest(new
                {
                    message = "Invalid date format."
                });
            }
 
 
            // --------------------------------------------------------
            // 2. Get all active seats
            // --------------------------------------------------------
 
            var seats = await _context.Seats
                .Where(s => s.IsActive)
                .OrderBy(s => s.ModuleId)
                .ThenBy(s => s.Section)
                .ThenBy(s => s.RowNumber)
                .ThenBy(s => s.ColumnNumber)
                .ToListAsync();
 
 
            // --------------------------------------------------------
            // 3. Get booked seats for selected date
            // --------------------------------------------------------
            //
            // Only Confirmed and CheckedIn seats are considered
            // occupied.
            //
            // Released, Cancelled and Expired seats are available.
            // --------------------------------------------------------
 
            var bookedSeatIds = await _context.HotseatBookings
                .Where(b =>
                    b.BookingDate == bookingDate &&
                    (
                        b.BookingStatus == "Confirmed" ||
                        b.BookingStatus == "CheckedIn"
                    ))
                .Select(b => b.SeatId)
                .ToListAsync();
 
 
            // --------------------------------------------------------
            // 4. Build seat availability response
            // --------------------------------------------------------
 
            var result = seats.Select(s => new HotseatSeatDto
            {
                SeatNumber = s.SeatNumber,
 
                Section = s.Section ?? "",
 
                Row = s.RowNumber,
 
                Status = bookedSeatIds.Contains(s.SeatId)
                    ? "Occupied"
                    : "Vacant"
 
            }).ToList();
 
 
            return Ok(result);
        }
 
 
        // ============================================================
        // POST: api/Hotseat
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> CreateBooking(
            [FromBody] CreateHotseatBookingDto request)
        {
            // --------------------------------------------------------
            // 1. Get employee ID from JWT
            // --------------------------------------------------------
 
            var employeeIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
 
            if (!int.TryParse(employeeIdClaim, out var employeeId))
            {
                return Unauthorized(new
                {
                    message = "Employee ID not found in token."
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
            // 3. Check whether seat is already booked
            // --------------------------------------------------------
 
            var existingSeatBooking =
                await _context.HotseatBookings
                    .FirstOrDefaultAsync(b =>
                        b.SeatId == request.SeatId &&
                        b.BookingDate == request.BookingDate &&
                        (
                            b.BookingStatus == "Confirmed" ||
                            b.BookingStatus == "CheckedIn"
                        ));
 
            if (existingSeatBooking != null)
            {
                return Conflict(new
                {
                    message =
                        "This seat is already booked for the selected date."
                });
            }
 
 
            // --------------------------------------------------------
            // 4. Check whether employee already has a booking
            // --------------------------------------------------------
 
            var existingEmployeeBooking =
                await _context.HotseatBookings
                    .FirstOrDefaultAsync(b =>
                        b.EmployeeId == employeeId &&
                        b.BookingDate == request.BookingDate &&
                        (
                            b.BookingStatus == "Confirmed" ||
                            b.BookingStatus == "CheckedIn"
                        ));
 
            if (existingEmployeeBooking != null)
            {
                return Conflict(new
                {
                    message =
                        "You already have a hotseat booking for the selected date."
                });
            }
 
 
            // --------------------------------------------------------
            // 5. Create check-in time
            // --------------------------------------------------------
            //
            // IMPORTANT:
            // PostgreSQL column is timestamp with time zone.
            //
            // Therefore we explicitly mark the DateTime as UTC.
            //
            // NOTE:
            // This currently treats ExpectedCheckInTime as UTC.
            // If your frontend sends IST, we can convert IST -> UTC
            // in the next step.
            // --------------------------------------------------------
 
            var expectedCheckIn = DateTime.SpecifyKind(
                request.BookingDate.ToDateTime(
                    request.ExpectedCheckInTime),
                DateTimeKind.Utc
            );
 
            var checkInDeadline =
                expectedCheckIn.AddMinutes(30);
 
 
            // --------------------------------------------------------
            // 6. Create booking entity
            // --------------------------------------------------------
 
            var booking = new HotseatBooking
            {
                SeatId = request.SeatId,
 
                EmployeeId = employeeId,
 
                BookingDate = request.BookingDate,
 
                BookingStatus = "Confirmed",
 
                BookedOn = DateTime.UtcNow,
 
                CheckInDeadline = checkInDeadline,
 
                CheckInTime = null,
 
                ReleasedOn = null,
 
                RecordIngestedBy = employeeId.ToString(),
 
                RecordIngestedOn = DateTime.UtcNow,
 
                RecordModifiedBy = null,
 
                RecordModifiedOn = null
            };
 
 
            // --------------------------------------------------------
            // 7. Save booking
            // --------------------------------------------------------
 
            _context.HotseatBookings.Add(booking);
 
            await _context.SaveChangesAsync();
 
 
            // --------------------------------------------------------
            // 8. Return booking details
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
 
                expectedCheckInTime =
                    request.ExpectedCheckInTime,
 
                checkInDeadline =
                    booking.CheckInDeadline
            });
        }
    }
}