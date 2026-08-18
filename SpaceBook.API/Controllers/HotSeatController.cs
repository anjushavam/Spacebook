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
            // Parse the selected date
            if (!DateOnly.TryParse(date, out var bookingDate))
            {
                return BadRequest("Invalid date format.");
            }
 
            // Get all active seats
            var seats = await _context.Seats
                .Where(s => s.IsActive)
                .OrderBy(s => s.ModuleId)
                .ThenBy(s => s.Section)
                .ThenBy(s => s.RowNumber)
                .ThenBy(s => s.ColumnNumber)
                .ToListAsync();
 
            // Get active bookings for the selected date
            var bookedSeatIds = await _context.HotseatBookings
                .Where(b =>
                    b.BookingDate == bookingDate &&
                    (b.BookingStatus == "Confirmed" ||
                     b.BookingStatus == "CheckedIn"))
                .Select(b => b.SeatId)
                .ToListAsync();
 
            // Build response
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
            // 1. Validate Seat
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
            // 2. Check whether the seat is already booked
            // --------------------------------------------------------
            var existingBooking = await _context.HotseatBookings
                .FirstOrDefaultAsync(b =>
                    b.SeatId == request.SeatId &&
                    b.BookingDate == request.BookingDate &&
                    (b.BookingStatus == "Confirmed" ||
                     b.BookingStatus == "CheckedIn"));
 
            if (existingBooking != null)
            {
                return Conflict(new
                {
                    message =
                        "This seat is already booked for the selected date."
                });
            }
 
 
            // --------------------------------------------------------
            // 3. Check whether employee already has a booking
            //    for the selected date
            // --------------------------------------------------------
 
            // TEMPORARY employee ID
            // Replace this with employee ID from JWT later.
            int employeeId = 105514;
 
            var employeeExistingBooking =
                await _context.HotseatBookings
                    .FirstOrDefaultAsync(b =>
                        b.EmployeeId == employeeId &&
                        b.BookingDate == request.BookingDate &&
                        (b.BookingStatus == "Confirmed" ||
                         b.BookingStatus == "CheckedIn"));
 
            if (employeeExistingBooking != null)
            {
                return Conflict(new
                {
                    message =
                        "You already have a hotseat booking for the selected date."
                });
            }
 
 
            // --------------------------------------------------------
            // 4. Calculate Check-In Deadline
            // --------------------------------------------------------
 
            // Expected check-in time from frontend
            //
            // Example:
            // 09:00 AM
            //      +
            // 30 minutes
            //      =
            // 09:30 AM deadline
 
            var expectedCheckIn =
                request.BookingDate.ToDateTime(
                    request.ExpectedCheckInTime);
 
            var checkInDeadline =
                expectedCheckIn.AddMinutes(30);
 
 
            // --------------------------------------------------------
            // 5. Create Hotseat Booking
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
            // 6. Save Booking
            // --------------------------------------------------------
 
            _context.HotseatBookings.Add(booking);
 
            await _context.SaveChangesAsync();
 
 
            // --------------------------------------------------------
            // 7. Return Booking Details
            // --------------------------------------------------------
 
            return Ok(new
            {
                message = "Hotseat booked successfully.",
 
                bookingId = booking.HotseatBookingId,
 
                seatId = booking.SeatId,
 
                seatNumber = seat.SeatNumber,
 
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