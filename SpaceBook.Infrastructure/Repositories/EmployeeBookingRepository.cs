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
    // CREATE BOOKING
    // =========================================================

    public async Task CreateBookingAsync(
        Booking booking)
    {
        await _context.Bookings.AddAsync(booking);
    }

    // =========================================================
    // SAVE CHANGES
    // =========================================================

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    // =========================================================
    // CHECK ROOM AVAILABILITY
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
    // CHECK ROOM AVAILABILITY
    // EXCLUDE EXISTING BOOKING
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
    // GET BOOKING DETAILS
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

                EmployeeId = b.EmployeeId,

                RoomName = b.Room != null ? b.Room.RoomName : string.Empty,

                Module = b.Room != null ? b.Room.Module : string.Empty,

                BookingDate = b.BookingDate,

                StartTime = b.StartTime,

                EndTime = b.EndTime,

                ParticipantCount = b.ParticipantCount,

                MeetingTitle = b.MeetingTitle,

                Purpose = b.Purpose,

                Status = b.Status,

                BookedOn = b.BookedOn
            })
            .FirstOrDefaultAsync();
    }

    // =========================================================
    // CANCEL BOOKING
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

        // Do not cancel an already cancelled booking
        if (string.Equals(
                booking.Status,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception(
                "This booking is already cancelled.");
        }

        booking.Status = "Cancelled";

        await _context.SaveChangesAsync();

        return true;
    }

    // =========================================================
    // UPDATE / RESCHEDULE BOOKING
    // =========================================================

    public async Task<bool> UpdateBookingAsync(
        int bookingId,
        int employeeId,
        UpdateBookingRequestDto request)
    {
        // -----------------------------------------------------
        // FIND EXISTING BOOKING
        // -----------------------------------------------------

        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b =>
                b.BookingId == bookingId &&
                b.EmployeeId == employeeId);

        if (booking == null)
        {
            return false;
        }

        // -----------------------------------------------------
        // PREVENT UPDATE OF CANCELLED BOOKING
        // -----------------------------------------------------

        if (string.Equals(
                booking.Status,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception(
                "Cancelled bookings cannot be updated.");
        }

        // -----------------------------------------------------
        // VALIDATE ROOM ID
        // -----------------------------------------------------

        if (!request.RoomId.HasValue ||
            request.RoomId.Value <= 0)
        {
            throw new Exception(
                "Room ID is required.");
        }

        // -----------------------------------------------------
        // GET ROOM
        // -----------------------------------------------------

        var room = await _context.Rooms
            .FirstOrDefaultAsync(r =>
                r.RoomId == request.RoomId.Value);

        if (room == null)
        {
            throw new Exception(
                "Selected room was not found.");
        }

        // -----------------------------------------------------
        // CHECK ROOM STATUS
        // -----------------------------------------------------

        if (room.IsBlocked ||
            room.Status == "Blocked")
        {
            throw new Exception(
                "Selected room is currently blocked.");
        }

        // -----------------------------------------------------
        // VALIDATE PARTICIPANT COUNT
        // -----------------------------------------------------

        if (request.ParticipantCount <= 0)
        {
            throw new Exception(
                "Participant count must be greater than zero.");
        }

        if (request.ParticipantCount > room.Capacity)
        {
            throw new Exception(
                $"Room capacity is {room.Capacity}. " +
                $"Participant count cannot exceed room capacity.");
        }

        // -----------------------------------------------------
        // VALIDATE BOOKING DATE
        // -----------------------------------------------------

        var today =
            DateOnly.FromDateTime(DateTime.Now);

        if (request.BookingDate < today)
        {
            throw new Exception(
                "Booking date cannot be in the past.");
        }

        // -----------------------------------------------------
        // WEEKEND VALIDATION
        // -----------------------------------------------------

        if (request.BookingDate.DayOfWeek ==
                DayOfWeek.Saturday ||
            request.BookingDate.DayOfWeek ==
                DayOfWeek.Sunday)
        {
            throw new Exception(
                "Bookings are not allowed on Saturdays and Sundays.");
        }

        // -----------------------------------------------------
        // VALIDATE START / END TIME
        // -----------------------------------------------------

        if (request.StartTime >= request.EndTime)
        {
            throw new Exception(
                "End time must be later than start time.");
        }

        // -----------------------------------------------------
        // CHECK BOOKING DURATION
        // -----------------------------------------------------

        var duration =
            request.EndTime - request.StartTime;

        if (duration.TotalMinutes <= 0)
        {
            throw new Exception(
                "Booking duration must be greater than zero.");
        }

        // -----------------------------------------------------
        // CHECK ROOM AVAILABILITY
        //
        // Exclude the current booking.
        // -----------------------------------------------------

        var roomAvailable =
            await IsRoomAvailableAsync(
                request.RoomId.Value,
                request.BookingDate,
                request.StartTime,
                request.EndTime,
                bookingId);

        if (!roomAvailable)
        {
            throw new Exception(
                "Selected room is already booked for the selected date and time.");
        }

        // -----------------------------------------------------
        // UPDATE ROOM
        // -----------------------------------------------------

        booking.RoomId =
            request.RoomId.Value;

        // -----------------------------------------------------
        // UPDATE DATE
        // -----------------------------------------------------

        booking.BookingDate =
            request.BookingDate;

        // -----------------------------------------------------
        // UPDATE START TIME
        // -----------------------------------------------------

        booking.StartTime =
            request.StartTime;

        // -----------------------------------------------------
        // UPDATE END TIME
        // -----------------------------------------------------

        booking.EndTime =
            request.EndTime;

        // -----------------------------------------------------
        // UPDATE MEETING TITLE
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
            request.MeetingTitle))
        {
            booking.MeetingTitle =
                request.MeetingTitle.Trim();
        }

        // -----------------------------------------------------
        // UPDATE PURPOSE
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
            request.Purpose))
        {
            booking.Purpose =
                request.Purpose.Trim();
        }

        // -----------------------------------------------------
        // UPDATE PARTICIPANT COUNT
        // -----------------------------------------------------

        booking.ParticipantCount =
            request.ParticipantCount;

        // -----------------------------------------------------
        // IMPORTANT:
        // EDITED BOOKINGS REQUIRE ADMIN APPROVAL AGAIN
        // -----------------------------------------------------

        booking.Status = "Pending";

        // -----------------------------------------------------
        // SAVE CHANGES
        // -----------------------------------------------------

        await _context.SaveChangesAsync();

        return true;
    }

    // =========================================================
    // SEARCH AVAILABLE ROOMS
    // =========================================================

    public async Task<List<AvailableRoomDto>>
        SearchAvailableRoomsAsync(
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
        // MODULE FILTER
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
            request.Module))
        {
            var module =
                request.Module.Trim();

            query = query.Where(r =>
                r.Module == module);
        }

        // -----------------------------------------------------
        // ROOM TYPE FILTER
        // -----------------------------------------------------

        if (request.RoomTypeId.HasValue &&
            request.RoomTypeId.Value > 0)
        {
            query = query.Where(r =>
                r.RoomTypeId ==
                request.RoomTypeId.Value);
        }

        // -----------------------------------------------------
        // PARTICIPANT CAPACITY FILTER
        // -----------------------------------------------------

        if (request.ParticipantCount.HasValue &&
            request.ParticipantCount.Value > 0)
        {
            query = query.Where(r =>
                r.Capacity >=
                request.ParticipantCount.Value);
        }

        // -----------------------------------------------------
        // FACILITY FILTER
        // -----------------------------------------------------

        if (request.FacilityIds != null &&
            request.FacilityIds.Count > 0)
        {
            foreach (var facilityId
                     in request.FacilityIds.Distinct())
            {
                query = query.Where(r =>
                    r.RoomFacilities.Any(rf =>
                        rf.FacilityId == facilityId));
            }
        }

        // -----------------------------------------------------
        // DATE + TIME AVAILABILITY
        // -----------------------------------------------------

        if (request.BookingDate.HasValue &&
            request.StartTime.HasValue &&
            request.EndTime.HasValue)
        {
            var bookingDate =
                request.BookingDate.Value;

            var startTime =
                request.StartTime.Value;

            var endTime =
                request.EndTime.Value;

            query = query.Where(room =>
                !room.Bookings.Any(booking =>
                    booking.BookingDate == bookingDate &&
                    booking.Status != "Cancelled" &&
                    booking.Status != "Rejected" &&
                    booking.StartTime < endTime &&
                    booking.EndTime > startTime
                ));
        }

        // -----------------------------------------------------
        // RETURN RESULT
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
                    .Where(rf =>
                        rf.Facility != null)
                    .Select(rf =>
                        rf.Facility!.FacilityName)
                    .ToList()
            })
            .ToListAsync();
    }

    // =========================================================
    // CHECK ROOM CAPACITY FOR SEARCH CRITERIA
    // =========================================================

    public async Task<bool>
        HasRoomWithRequiredCapacityAsync(
            SearchRoomsRequestDto request)
    {
        if (!request.ParticipantCount.HasValue ||
            request.ParticipantCount.Value <= 0)
        {
            return true;
        }

        var query = _context.Rooms
            .Where(r =>
                !r.IsBlocked &&
                r.Status != "Blocked")
            .AsQueryable();

        // -----------------------------------------------------
        // MODULE FILTER
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
            request.Module))
        {
            var module =
                request.Module.Trim();

            query = query.Where(r =>
                r.Module == module);
        }

        // -----------------------------------------------------
        // ROOM TYPE FILTER
        // -----------------------------------------------------

        if (request.RoomTypeId.HasValue &&
            request.RoomTypeId.Value > 0)
        {
            query = query.Where(r =>
                r.RoomTypeId ==
                request.RoomTypeId.Value);
        }

        // -----------------------------------------------------
        // FACILITY FILTER
        // -----------------------------------------------------

        if (request.FacilityIds != null &&
            request.FacilityIds.Count > 0)
        {
            foreach (var facilityId
                     in request.FacilityIds.Distinct())
            {
                query = query.Where(r =>
                    r.RoomFacilities.Any(rf =>
                        rf.FacilityId == facilityId));
            }
        }

        // -----------------------------------------------------
        // CHECK CAPACITY
        // -----------------------------------------------------

        return await query.AnyAsync(r =>
            r.Capacity >=
            request.ParticipantCount.Value);
    }

    // =========================================================
    // GET ROOMS BY MODULE
    // =========================================================

    public async Task<List<AvailableRoomDto>>
        GetRoomsByModuleAsync(
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
                    .Where(rf =>
                        rf.Facility != null)
                    .Select(rf =>
                        rf.Facility!.FacilityName)
                    .ToList()
            })
            .ToListAsync();
    }

    // =========================================================
    // GET ROOM CAPACITY
    // =========================================================

    public async Task<int?> GetRoomCapacityAsync(
        int roomId)
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