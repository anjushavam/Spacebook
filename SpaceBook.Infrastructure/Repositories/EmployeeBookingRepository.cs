using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Booking;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
using SpaceBook.Infrastructure.Data;

namespace SpaceBook.Infrastructure.Repositories;

public class EmployeeBookingRepository : IEmployeeBookingRepository
{
    private readonly ApplicationDbContext _context;

    // =========================================================
    // OFFICE HOURS
    // =========================================================
    // Rooms can only be searched/booked between:
    //
    // 10:00 AM and 07:30 PM
    // =========================================================

    private static readonly TimeOnly OfficeStartTime =
        new TimeOnly(10, 0);

    private static readonly TimeOnly OfficeEndTime =
        new TimeOnly(19, 30);

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
        if (booking == null)
        {
            throw new Exception(
                "Booking is required.");
        }

        // -----------------------------------------------------
        // VALIDATE ROOM ID
        // -----------------------------------------------------

        if (booking.RoomId <= 0)
        {
            throw new Exception(
                "Room ID is required.");
        }

        // -----------------------------------------------------
        // VALIDATE EMPLOYEE ID
        // -----------------------------------------------------

        if (booking.EmployeeId <= 0)
        {
            throw new Exception(
                "Employee ID is required.");
        }

        // -----------------------------------------------------
        // VALIDATE BOOKING DATE
        // -----------------------------------------------------

        if (booking.BookingDate.DayOfWeek ==
                DayOfWeek.Saturday ||
            booking.BookingDate.DayOfWeek ==
                DayOfWeek.Sunday)
        {
            throw new Exception(
                "Bookings are not allowed on Saturdays and Sundays.");
        }

        // -----------------------------------------------------
        // VALIDATE OFFICE HOURS
        // -----------------------------------------------------

        if (booking.StartTime < OfficeStartTime)
        {
            throw new Exception(
                "Bookings can only start from 10:00 AM.");
        }

        if (booking.EndTime > OfficeEndTime)
        {
            throw new Exception(
                "Bookings must end by 07:30 PM.");
        }

        if (booking.StartTime >= booking.EndTime)
        {
            throw new Exception(
                "End time must be later than start time.");
        }

        // -----------------------------------------------------
        // VALIDATE PARTICIPANT COUNT
        // -----------------------------------------------------

        if (booking.ParticipantCount <= 0)
        {
            throw new Exception(
                "Participant count must be greater than zero.");
        }

        // -----------------------------------------------------
        // CHECK ROOM
        // -----------------------------------------------------

        var room = await _context.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.RoomId == booking.RoomId);

        if (room == null)
        {
            throw new Exception(
                "Selected room was not found.");
        }

        // -----------------------------------------------------
        // CHECK ROOM STATUS
        // -----------------------------------------------------

        if (room.IsBlocked ||
            string.Equals(
                room.Status,
                "Blocked",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception(
                "Selected room is currently blocked.");
        }

        // -----------------------------------------------------
        // CHECK ROOM CAPACITY
        // -----------------------------------------------------

        if (booking.ParticipantCount > room.Capacity)
        {
            throw new Exception(
                $"The selected room can accommodate a maximum of {room.Capacity} participants.");
        }

        // -----------------------------------------------------
        // CHECK ROOM AVAILABILITY
        // -----------------------------------------------------

        var roomAvailable =
            await IsRoomAvailableAsync(
                booking.RoomId,
                booking.BookingDate,
                booking.StartTime,
                booking.EndTime);

        if (!roomAvailable)
        {
            throw new Exception(
                "Room is already booked for the selected date and time.");
        }

        // -----------------------------------------------------
        // NORMALIZE BOOKED ON
        // -----------------------------------------------------
        // PostgreSQL column:
        //
        // timestamp with time zone
        //
        // Therefore the DateTime MUST be UTC.
        //
        // IMPORTANT:
        // Do NOT use DateTimeKind.Unspecified here.
        // -----------------------------------------------------

        booking.BookedOn = DateTime.UtcNow;

        // -----------------------------------------------------
        // NEW BOOKING
        // -----------------------------------------------------

        booking.CancellationReason = null;

        // -----------------------------------------------------
        // ADD BOOKING
        // -----------------------------------------------------

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
        // -----------------------------------------------------
        // VALIDATE ROOM ID
        // -----------------------------------------------------

        if (roomId <= 0)
        {
            throw new Exception(
                "Invalid room ID.");
        }

        // -----------------------------------------------------
        // VALIDATE OFFICE HOURS
        // -----------------------------------------------------

        if (startTime < OfficeStartTime)
        {
            throw new Exception(
                "Bookings can only start from 10:00 AM.");
        }

        if (endTime > OfficeEndTime)
        {
            throw new Exception(
                "Bookings must end by 07:30 PM.");
        }

        if (startTime >= endTime)
        {
            throw new Exception(
                "End time must be later than start time.");
        }

        // -----------------------------------------------------
        // CHECK OVERLAPPING BOOKINGS
        // -----------------------------------------------------
        //
        // Pending and Approved bookings block the room.
        //
        // Cancelled and Rejected bookings do not block it.
        //
        // Overlap:
        //
        // Existing Start < Requested End
        // Existing End   > Requested Start
        // -----------------------------------------------------

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
        // -----------------------------------------------------
        // VALIDATE ROOM ID
        // -----------------------------------------------------

        if (roomId <= 0)
        {
            throw new Exception(
                "Invalid room ID.");
        }

        // -----------------------------------------------------
        // VALIDATE OFFICE HOURS
        // -----------------------------------------------------

        if (startTime < OfficeStartTime)
        {
            throw new Exception(
                "Bookings can only start from 10:00 AM.");
        }

        if (endTime > OfficeEndTime)
        {
            throw new Exception(
                "Bookings must end by 07:30 PM.");
        }

        if (startTime >= endTime)
        {
            throw new Exception(
                "End time must be later than start time.");
        }

        // -----------------------------------------------------
        // CHECK OVERLAPPING BOOKINGS
        // EXCLUDE CURRENT BOOKING
        // -----------------------------------------------------

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
        if (bookingId <= 0)
        {
            throw new Exception(
                "Invalid booking ID.");
        }

        if (employeeId <= 0)
        {
            throw new Exception(
                "Invalid employee.");
        }

        return await _context.Bookings
            .AsNoTracking()

            .Include(b => b.Room)
                .ThenInclude(r => r!.RoomType)

            .Include(b => b.Room)
                .ThenInclude(r => r!.Module)

            .Include(b => b.Room)
                .ThenInclude(r => r!.RoomFacilities)
                    .ThenInclude(rf => rf.Facility)

            .Where(b =>
                b.BookingId == bookingId &&
                b.EmployeeId == employeeId)

            .Select(b => new BookingDetailsDto
            {
                BookingId =
                    b.BookingId,

                EmployeeId =
                    b.EmployeeId,

                RoomName =
                    b.Room != null
                        ? b.Room.RoomName
                        : string.Empty,

                Module =
                    b.Room != null &&
                    b.Room.Module != null
                        ? b.Room.Module.ModuleName
                        : string.Empty,

                BookingDate =
                    b.BookingDate,

                StartTime =
                    b.StartTime,

                EndTime =
                    b.EndTime,

                ParticipantCount =
                    b.ParticipantCount,

                MeetingTitle =
                    b.MeetingTitle,

                Purpose =
                    b.Purpose,

                Status =
                    b.Status,

                BookedOn =
                    b.BookedOn
            })

            .FirstOrDefaultAsync();
    }

    // =========================================================
    // CANCEL BOOKING
    // =========================================================

    public async Task<bool> CancelBookingAsync(
        int bookingId,
        int employeeId,
        string reason)
    {
        // -----------------------------------------------------
        // VALIDATE BOOKING ID
        // -----------------------------------------------------

        if (bookingId <= 0)
        {
            throw new Exception(
                "Invalid booking ID.");
        }

        // -----------------------------------------------------
        // VALIDATE EMPLOYEE ID
        // -----------------------------------------------------

        if (employeeId <= 0)
        {
            throw new Exception(
                "Invalid employee.");
        }

        // -----------------------------------------------------
        // VALIDATE REASON
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new Exception(
                "Cancellation reason is required.");
        }

        var cancellationReason =
            reason.Trim();

        // -----------------------------------------------------
        // MAXIMUM REASON LENGTH
        // -----------------------------------------------------

        if (cancellationReason.Length > 500)
        {
            throw new Exception(
                "Cancellation reason cannot exceed 500 characters.");
        }

        // -----------------------------------------------------
        // FIND BOOKING
        // -----------------------------------------------------

        var booking =
            await _context.Bookings
                .FirstOrDefaultAsync(b =>
                    b.BookingId == bookingId &&
                    b.EmployeeId == employeeId);

        // -----------------------------------------------------
        // BOOKING NOT FOUND
        // -----------------------------------------------------

        if (booking == null)
        {
            return false;
        }

        // -----------------------------------------------------
        // ALREADY CANCELLED
        // -----------------------------------------------------

        if (string.Equals(
                booking.Status,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception(
                "This booking is already cancelled.");
        }

        // -----------------------------------------------------
        // PREVENT CANCELLING REJECTED BOOKING
        // -----------------------------------------------------

        if (string.Equals(
                booking.Status,
                "Rejected",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception(
                "Rejected bookings cannot be cancelled.");
        }

        // -----------------------------------------------------
        // CANCEL BOOKING
        // -----------------------------------------------------

        booking.Status = "Cancelled";

        booking.CancellationReason =
            cancellationReason;

        // -----------------------------------------------------
        // SAVE
        // -----------------------------------------------------

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
        // VALIDATE REQUEST
        // -----------------------------------------------------

        if (request == null)
        {
            throw new Exception(
                "Update booking request is required.");
        }

        // -----------------------------------------------------
        // FIND EXISTING BOOKING
        // -----------------------------------------------------

        var booking =
            await _context.Bookings
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
        // PREVENT UPDATE OF REJECTED BOOKING
        // -----------------------------------------------------

        if (string.Equals(
                booking.Status,
                "Rejected",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception(
                "Rejected bookings cannot be updated.");
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

        var room =
            await _context.Rooms
                .AsNoTracking()
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
            string.Equals(
                room.Status,
                "Blocked",
                StringComparison.OrdinalIgnoreCase))
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
        // SAME-DAY START TIME VALIDATION
        // -----------------------------------------------------

        if (request.BookingDate == today)
        {
            var currentTime =
                TimeOnly.FromDateTime(DateTime.Now);

            if (request.StartTime <= currentTime)
            {
                throw new Exception(
                    "Booking cannot be rescheduled to a time that has already passed.");
            }
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
        // OFFICE HOURS
        // -----------------------------------------------------

        if (request.StartTime < OfficeStartTime)
        {
            throw new Exception(
                "Bookings can only start from 10:00 AM.");
        }

        if (request.EndTime > OfficeEndTime)
        {
            throw new Exception(
                "Bookings must end by 07:30 PM.");
        }

        // -----------------------------------------------------
        // CHECK ROOM AVAILABILITY
        // EXCLUDE CURRENT BOOKING
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
        // CLEAR CANCELLATION DATA
        // -----------------------------------------------------

        booking.CancellationReason =
            null;

        // -----------------------------------------------------
        // RESCHEDULE REQUIRES ADMIN APPROVAL
        // -----------------------------------------------------

        booking.Status =
            "Pending";

        // -----------------------------------------------------
        // SAVE
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
        // -----------------------------------------------------
        // VALIDATE REQUEST
        // -----------------------------------------------------

        if (request == null)
        {
            throw new Exception(
                "Search request is required.");
        }

        // -----------------------------------------------------
        // VALIDATE START / END TIME
        // -----------------------------------------------------

        if (request.StartTime.HasValue !=
            request.EndTime.HasValue)
        {
            throw new Exception(
                "Both start time and end time are required when searching by time.");
        }

        if (request.StartTime.HasValue &&
            request.EndTime.HasValue)
        {
            var startTime =
                request.StartTime.Value;

            var endTime =
                request.EndTime.Value;

            if (startTime >= endTime)
            {
                throw new Exception(
                    "End time must be later than start time.");
            }

            if (startTime < OfficeStartTime)
            {
                throw new Exception(
                    "Rooms can only be searched from 10:00 AM.");
            }

            if (endTime > OfficeEndTime)
            {
                throw new Exception(
                    "Rooms can only be searched until 07:30 PM.");
            }
        }

        // -----------------------------------------------------
        // VALIDATE DATE
        // -----------------------------------------------------

        if (request.BookingDate.HasValue)
        {
            var bookingDate =
                request.BookingDate.Value;

            if (bookingDate.DayOfWeek ==
                    DayOfWeek.Saturday ||
                bookingDate.DayOfWeek ==
                    DayOfWeek.Sunday)
            {
                throw new Exception(
                    "Room availability is not allowed on Saturdays and Sundays.");
            }

            var today =
                DateOnly.FromDateTime(DateTime.Now);

            if (bookingDate < today)
            {
                throw new Exception(
                    "Cannot search availability for a past date.");
            }

            // -------------------------------------------------
            // SAME-DAY TIME VALIDATION
            // -------------------------------------------------

            if (request.StartTime.HasValue &&
                request.EndTime.HasValue &&
                bookingDate == today)
            {
                var currentTime =
                    TimeOnly.FromDateTime(DateTime.Now);

                if (request.StartTime.Value <= currentTime)
                {
                    throw new Exception(
                        "Cannot search for a time that has already passed.");
                }
            }
        }

        // -----------------------------------------------------
        // PARTICIPANT COUNT
        // -----------------------------------------------------

        if (request.ParticipantCount.HasValue &&
            request.ParticipantCount.Value <= 0)
        {
            throw new Exception(
                "Participant count must be greater than zero.");
        }

        // -----------------------------------------------------
        // START QUERY
        // -----------------------------------------------------

        var query =
            _context.Rooms
                .AsNoTracking()

                .Include(r => r.RoomType)

                .Include(r => r.Module)

                .Include(r => r.RoomFacilities)
                    .ThenInclude(rf => rf.Facility)

                .Where(r =>
                    !r.IsBlocked &&
                    r.Status != "Blocked")

                .AsQueryable();

        // =====================================================
        // MODULE FILTER
        // =====================================================

        if (!string.IsNullOrWhiteSpace(
            request.Module))
        {
            var module =
                request.Module.Trim();

            query = query.Where(r =>
                r.Module != null &&
                r.Module.ModuleName == module);
        }

        // =====================================================
        // ROOM TYPE FILTER
        // =====================================================

        if (request.RoomTypeId.HasValue &&
            request.RoomTypeId.Value > 0)
        {
            query = query.Where(r =>
                r.RoomTypeId ==
                request.RoomTypeId.Value);
        }

        // =====================================================
        // PARTICIPANT CAPACITY FILTER
        // =====================================================

        if (request.ParticipantCount.HasValue &&
            request.ParticipantCount.Value > 0)
        {
            query = query.Where(r =>
                r.Capacity >=
                request.ParticipantCount.Value);
        }

        // =====================================================
        // FACILITY FILTER
        // =====================================================

        if (request.FacilityIds != null &&
            request.FacilityIds.Count > 0)
        {
            foreach (var facilityId
                     in request.FacilityIds.Distinct())
            {
                if (facilityId > 0)
                {
                    query = query.Where(r =>
                        r.RoomFacilities.Any(rf =>
                            rf.FacilityId == facilityId));
                }
            }
        }

        // =====================================================
        // DATE + TIME AVAILABILITY
        // =====================================================

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

                    booking.BookingDate ==
                    bookingDate &&

                    // Pending and Approved bookings
                    // block the room.
                    booking.Status != "Cancelled" &&
                    booking.Status != "Rejected" &&

                    // OVERLAP CHECK
                    booking.StartTime < endTime &&
                    booking.EndTime > startTime
                ));
        }

        // =====================================================
        // RETURN RESULT
        // =====================================================

        return await query
            .Select(r => new AvailableRoomDto
            {
                RoomId =
                    r.RoomId,

                RoomName =
                    r.RoomName,

                Module =
                    r.Module != null
                        ? r.Module.ModuleName
                        : string.Empty,

                RoomType =
                    r.RoomType != null
                        ? r.RoomType.TypeName
                        : string.Empty,

                Capacity =
                    r.Capacity,

                Facilities =
                    r.RoomFacilities
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
        if (request == null)
        {
            throw new Exception(
                "Search request is required.");
        }

        if (!request.ParticipantCount.HasValue ||
            request.ParticipantCount.Value <= 0)
        {
            return true;
        }

        var query =
            _context.Rooms
                .AsNoTracking()
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
                r.Module != null &&
                r.Module.ModuleName == module);
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
                if (facilityId > 0)
                {
                    query = query.Where(r =>
                        r.RoomFacilities.Any(rf =>
                            rf.FacilityId == facilityId));
                }
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

        var moduleName =
            module.Trim();

        return await _context.Rooms
            .AsNoTracking()

            .Include(r => r.RoomType)

            .Include(r => r.Module)

            .Include(r => r.RoomFacilities)
                .ThenInclude(rf => rf.Facility)

            .Where(r =>
                !r.IsBlocked &&
                r.Status != "Blocked" &&

                r.Module != null &&
                r.Module.ModuleName == moduleName)

            .Select(r => new AvailableRoomDto
            {
                RoomId =
                    r.RoomId,

                RoomName =
                    r.RoomName,

                Module =
                    r.Module != null
                        ? r.Module.ModuleName
                        : string.Empty,

                RoomType =
                    r.RoomType != null
                        ? r.RoomType.TypeName
                        : string.Empty,

                Capacity =
                    r.Capacity,

                Facilities =
                    r.RoomFacilities
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
        if (roomId <= 0)
        {
            return null;
        }

        return await _context.Rooms
            .AsNoTracking()

            .Where(r =>
                r.RoomId == roomId &&

                !r.IsBlocked &&

                r.Status != "Blocked")

            .Select(r =>
                (int?)r.Capacity)

            .FirstOrDefaultAsync();
    }
}