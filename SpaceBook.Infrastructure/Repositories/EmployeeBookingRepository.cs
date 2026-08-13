using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Booking;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
using SpaceBook.Infrastructure.Data;

namespace SpaceBook.Infrastructure.Repositories;

public class EmployeeBookingRepository : IEmployeeBookingRepository
{
    private readonly ApplicationDbContext _context;

    public EmployeeBookingRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }


    // =========================================================
    // Create Booking
    // =========================================================

    public async Task CreateBookingAsync(
        Booking booking)
    {
        await _context.Bookings.AddAsync(booking);
    }


    // =========================================================
    // Save Changes
    // =========================================================

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }


    // =========================================================
    // Check Room Availability
    // =========================================================

    public async Task<bool> IsRoomAvailableAsync(
        int roomId,
        DateOnly bookingDate,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        return !await _context.Bookings
            .AnyAsync(b =>
                b.RoomId == roomId &&
                b.BookingDate == bookingDate &&

                b.Status != "Cancelled" &&
                b.Status != "Rejected" &&

                b.StartTime < endTime &&
                b.EndTime > startTime
            );
    }


    // =========================================================
    // Check Room Availability
    // Exclude Existing Booking
    // =========================================================

    public async Task<bool> IsRoomAvailableAsync(
        int roomId,
        DateOnly bookingDate,
        TimeOnly startTime,
        TimeOnly endTime,
        int excludeBookingId)
    {
        return !await _context.Bookings
            .AnyAsync(b =>
                b.RoomId == roomId &&
                b.BookingId != excludeBookingId &&
                b.BookingDate == bookingDate &&

                b.Status != "Cancelled" &&
                b.Status != "Rejected" &&

                b.StartTime < endTime &&
                b.EndTime > startTime
            );
    }


    // =========================================================
    // Get Booking Details
    // =========================================================

    public async Task<BookingDetailsDto?> GetBookingByIdAsync(
    int bookingId,
    int employeeId)
{
    return await _context.Bookings
        .Include(b => b.Room)
            .ThenInclude(r => r!.RoomType)

        .Include(b => b.Room)
            .ThenInclude(r => r!.RoomFacilities)
                .ThenInclude(rf => rf.Facility)

        .Where(b =>
            b.BookingId == bookingId &&
            b.EmployeeId == employeeId)

        .Select(b => new BookingDetailsDto
        {
            BookingId = b.BookingId,

            RoomName = b.Room!.RoomName,

            BookingDate = b.BookingDate,

            StartTime = b.StartTime,

            EndTime = b.EndTime,

            ParticipantCount = b.ParticipantCount,

            MeetingTitle = b.MeetingTitle,

            Purpose = b.Purpose,

            Status = b.Status
        })

        .FirstOrDefaultAsync();
}


    // =========================================================
    // Cancel Booking
    // =========================================================

    public async Task<bool> CancelBookingAsync(
        int bookingId,
        int employeeId)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b =>
                b.BookingId == bookingId &&
                b.EmployeeId == employeeId);

        if (booking == null)
        {
            return false;
        }

        booking.Status = "Cancelled";

        await _context.SaveChangesAsync();

        return true;
    }


    // =========================================================
    // Update / Reschedule Booking
    // =========================================================

    // =========================================================
// Update / Reschedule Booking
// =========================================================

public async Task<bool> UpdateBookingAsync(
    int bookingId,
    int employeeId,
    UpdateBookingRequestDto request)
{
    var booking = await _context.Bookings
        .FirstOrDefaultAsync(b =>
            b.BookingId == bookingId &&
            b.EmployeeId == employeeId);

    if (booking == null)
    {
        return false;
    }

    // RoomId is nullable in UpdateBookingRequestDto,
    // but Booking.RoomId is non-nullable int.
    if (!request.RoomId.HasValue ||
        request.RoomId.Value <= 0)
    {
        throw new Exception(
            "Room ID is required.");
    }

    booking.RoomId = request.RoomId.Value;

    booking.BookingDate = request.BookingDate;

    booking.StartTime = request.StartTime;

    booking.EndTime = request.EndTime;

    if (!string.IsNullOrWhiteSpace(request.MeetingTitle))
    {
        booking.MeetingTitle = request.MeetingTitle;
    }

    if (!string.IsNullOrWhiteSpace(request.Purpose))
    {
        booking.Purpose = request.Purpose;
    }

    booking.ParticipantCount =
        request.ParticipantCount;

    await _context.SaveChangesAsync();

    return true;
}

    // =========================================================
    // Search Available Rooms
    // =========================================================

    public async Task<List<AvailableRoomDto>> SearchAvailableRoomsAsync(
        SearchRoomsRequestDto request)
    {
        var query = _context.Rooms
            .Include(r => r.RoomType)
            .Include(r => r.RoomFacilities)
                .ThenInclude(rf => rf.Facility)
            .Where(r =>
                !r.IsBlocked &&
                r.Status != "Blocked")
            .AsQueryable();


        // -----------------------------------------------------
        // Module Filter
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(request.Module))
        {
            query = query.Where(r =>
                r.Module == request.Module);
        }


        // -----------------------------------------------------
        // Room Type Filter
        // -----------------------------------------------------

        if (request.RoomTypeId.HasValue &&
            request.RoomTypeId.Value > 0)
        {
            query = query.Where(r =>
                r.RoomTypeId == request.RoomTypeId.Value);
        }


        // -----------------------------------------------------
        // Participant Capacity Filter
        // -----------------------------------------------------

        if (request.ParticipantCount.HasValue &&
            request.ParticipantCount.Value > 0)
        {
            query = query.Where(r =>
                r.Capacity >= request.ParticipantCount.Value);
        }


        // -----------------------------------------------------
        // Facility Filter
        // -----------------------------------------------------

        if (request.FacilityIds != null &&
            request.FacilityIds.Count > 0)
        {
            foreach (var facilityId in request.FacilityIds)
            {
                query = query.Where(r =>
                    r.RoomFacilities.Any(rf =>
                        rf.FacilityId == facilityId));
            }
        }


        // -----------------------------------------------------
        // Date + Time Availability
        // -----------------------------------------------------

        if (request.BookingDate.HasValue)
        {
            var bookingDate = request.BookingDate.Value;

            if (request.StartTime.HasValue &&
                request.EndTime.HasValue)
            {
                var startTime = request.StartTime.Value;
                var endTime = request.EndTime.Value;

                query = query.Where(room =>
                    !room.Bookings.Any(booking =>

                        booking.BookingDate == bookingDate &&

                        booking.Status != "Cancelled" &&
                        booking.Status != "Rejected" &&

                        booking.StartTime < endTime &&
                        booking.EndTime > startTime
                    ));
            }
        }


        // -----------------------------------------------------
        // Select Result
        // -----------------------------------------------------

        return await query
            .Select(r => new AvailableRoomDto
            {
                RoomId = r.RoomId,

                RoomName = r.RoomName,

                Module = r.Module,

                RoomType = r.RoomType != null
                    ? r.RoomType.TypeName
                    : string.Empty,

                Capacity = r.Capacity,

                Facilities = r.RoomFacilities
                    .Where(rf => rf.Facility != null)
                    .Select(rf =>
                        rf.Facility!.FacilityName)
                    .ToList()
            })
            .ToListAsync();
    }


    // =========================================================
    // Get Rooms By Module
    // =========================================================
    //
    // This method is required by
    // IEmployeeBookingRepository.
    //
    // Example:
    // GET rooms for "Module 2"
    //
    // It does NOT check booking date/time.
    // It simply returns rooms belonging to that module.
    // =========================================================

    public async Task<List<AvailableRoomDto>> GetRoomsByModuleAsync(
        string module)
    {
        if (string.IsNullOrWhiteSpace(module))
        {
            return new List<AvailableRoomDto>();
        }

        module = module.Trim();

        return await _context.Rooms
            .Include(r => r.RoomType)

            .Include(r => r.RoomFacilities)
                .ThenInclude(rf => rf.Facility)

            .Where(r =>
                !r.IsBlocked &&
                r.Status != "Blocked" &&
                r.Module == module)

            .Select(r => new AvailableRoomDto
            {
                RoomId = r.RoomId,

                RoomName = r.RoomName,

                Module = r.Module,

                RoomType = r.RoomType != null
                    ? r.RoomType.TypeName
                    : string.Empty,

                Capacity = r.Capacity,

                Facilities = r.RoomFacilities
                    .Where(rf => rf.Facility != null)
                    .Select(rf =>
                        rf.Facility!.FacilityName)
                    .ToList()
            })

            .ToListAsync();
    }

// =========================================================
// Get Room Capacity
// =========================================================

public async Task<int?> GetRoomCapacityAsync(int roomId)
{
    return await _context.Rooms
        .Where(r =>
            r.RoomId == roomId &&
            !r.IsBlocked &&
            r.Status != "Blocked")
        .Select(r => (int?)r.Capacity)
        .FirstOrDefaultAsync();
}
}