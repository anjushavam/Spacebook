using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Booking;
using SpaceBook.Application.DTOs.Room;
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
    // SpaceBook business hours are based on IST.
    //
    // 10:00 AM to 10:00 PM
    // =========================================================

    private static readonly TimeOnly OfficeStartTime =
        new TimeOnly(10, 0);

    private static readonly TimeOnly OfficeEndTime =
        new TimeOnly(22, 0);

    // =========================================================
    // INDIA TIMEZONE
    // =========================================================

    private static readonly TimeZoneInfo IndiaTimeZone =
        GetIndiaTimeZone();

    private static TimeZoneInfo GetIndiaTimeZone()
    {
        try
        {
            // Linux / Render
            return TimeZoneInfo.FindSystemTimeZoneById(
                "Asia/Kolkata");
        }
        catch (TimeZoneNotFoundException)
        {
            // Windows
            return TimeZoneInfo.FindSystemTimeZoneById(
                "India Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            // Windows fallback
            return TimeZoneInfo.FindSystemTimeZoneById(
                "India Standard Time");
        }
    }

    // =========================================================
    // GET CURRENT INDIA DATE/TIME
    // =========================================================

    private static DateTime GetIndiaNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            IndiaTimeZone);
    }

    // =========================================================
    // GET CURRENT INDIA DATE
    // =========================================================

    private static DateOnly GetIndiaToday()
    {
        return DateOnly.FromDateTime(
            GetIndiaNow());
    }

    // =========================================================
    // GET CURRENT INDIA TIME
    // =========================================================

    private static TimeOnly GetIndiaCurrentTime()
    {
        return TimeOnly.FromDateTime(
            GetIndiaNow());
    }

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

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
        // VALIDATE MEETING TITLE
        // -----------------------------------------------------
        //
        // MeetingTitle is now the field used for the booking
        // title. Do not use Purpose here.
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(
            booking.MeetingTitle))
        {
            throw new Exception(
                "Meeting title is required.");
        }

        booking.MeetingTitle =
            booking.MeetingTitle.Trim();

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
        // VALIDATE PAST DATE
        // -----------------------------------------------------

        var today = GetIndiaToday();

        if (booking.BookingDate < today)
        {
            throw new Exception(
                "Bookings cannot be created for a past date.");
        }

        // -----------------------------------------------------
        // VALIDATE SAME-DAY TIME
        // -----------------------------------------------------

        if (booking.BookingDate == today)
        {
            var currentTime =
                GetIndiaCurrentTime();

            if (booking.StartTime <= currentTime)
            {
                throw new Exception(
                    "Bookings cannot start at or before the current time.");
            }
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
                "Bookings must end by 10:00 PM.");
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

        var room =
            await _context.Rooms
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
        //
        // PostgreSQL timestamp with time zone requires UTC.
        // -----------------------------------------------------

        booking.BookedOn =
            DateTime.UtcNow;

        // -----------------------------------------------------
        // NEW BOOKING
        // -----------------------------------------------------

        booking.CancellationReason = null;

        // -----------------------------------------------------
        // AUTO APPROVE BOOKING
        // -----------------------------------------------------

        booking.Status =
            "Approved";

        // -----------------------------------------------------
        // ADD BOOKING
        // -----------------------------------------------------

        await _context.Bookings.AddAsync(
            booking);
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
        // VALIDATE DATE
        // -----------------------------------------------------

        if (bookingDate.DayOfWeek ==
                DayOfWeek.Saturday ||
            bookingDate.DayOfWeek ==
                DayOfWeek.Sunday)
        {
            throw new Exception(
                "Room bookings are not allowed on Saturdays and Sundays.");
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
                "Bookings must end by 10:00 PM.");
        }

        if (startTime >= endTime)
        {
            throw new Exception(
                "End time must be later than start time.");
        }

        // -----------------------------------------------------
        // VALIDATE PAST DATE
        // -----------------------------------------------------

        var today =
            GetIndiaToday();

        if (bookingDate < today)
        {
            throw new Exception(
                "Cannot check availability for a past date.");
        }

        // -----------------------------------------------------
        // CHECK OVERLAPPING BOOKINGS
        // -----------------------------------------------------
        //
        // Existing booking:
        //
        // Start < Requested End
        // End   > Requested Start
        //
        // Cancelled and Rejected bookings do not block rooms.
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
        if (roomId <= 0)
        {
            throw new Exception(
                "Invalid room ID.");
        }

        // -----------------------------------------------------
        // VALIDATE DATE
        // -----------------------------------------------------

        if (bookingDate.DayOfWeek ==
                DayOfWeek.Saturday ||
            bookingDate.DayOfWeek ==
                DayOfWeek.Sunday)
        {
            throw new Exception(
                "Room bookings are not allowed on Saturdays and Sundays.");
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
                "Bookings must end by 10:00 PM.");
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
                // -------------------------------------------------
                // BOOKING INFORMATION
                // -------------------------------------------------

                BookingId =
                    b.BookingId,

                EmployeeId =
                    b.EmployeeId,

                RoomId =
                    b.RoomId,

                // -------------------------------------------------
                // ROOM INFORMATION
                // -------------------------------------------------

                RoomName =
                    b.Room != null
                        ? b.Room.RoomName
                        : string.Empty,

                Module =
                    b.Room != null &&
                    b.Room.Module != null
                        ? b.Room.Module.ModuleName
                        : string.Empty,

                // -------------------------------------------------
                // DATE / TIME
                // -------------------------------------------------

                BookingDate =
                    b.BookingDate,

                StartTime =
                    b.StartTime,

                EndTime =
                    b.EndTime,

                // -------------------------------------------------
                // MEETING INFORMATION
                // -------------------------------------------------

                MeetingTitle =
                    b.MeetingTitle ?? string.Empty,

                ParticipantCount =
                    b.ParticipantCount,

                // -------------------------------------------------
                // STATUS
                // -------------------------------------------------

                Status =
                    b.Status,

                // -------------------------------------------------
                // BOOKED ON
                // -------------------------------------------------

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

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new Exception(
                "Cancellation reason is required.");
        }

        var cancellationReason =
            reason.Trim();

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

        booking.Status =
            "Cancelled";

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
        // FALLBACK / VALIDATE ROOM ID
        // -----------------------------------------------------

        if (!request.RoomId.HasValue ||
            request.RoomId.Value <= 0)
        {
            request.RoomId = booking.RoomId;
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
        // FALLBACK / VALIDATE PARTICIPANT COUNT
        // -----------------------------------------------------

        if (request.ParticipantCount <= 0)
        {
            request.ParticipantCount = booking.ParticipantCount;
        }

        if (request.ParticipantCount > room.Capacity)
        {
            throw new Exception(
                $"Room capacity is {room.Capacity}. " +
                $"Participant count cannot exceed room capacity.");
        }

        // -----------------------------------------------------
        // VALIDATE DATE
        // -----------------------------------------------------

        var today =
            GetIndiaToday();

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
                GetIndiaCurrentTime();

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
                "Bookings must end by 10:00 PM.");
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
        //
        // Only update if a title was supplied.
        // Existing title is preserved otherwise.
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
            request.MeetingTitle))
        {
            booking.MeetingTitle =
                request.MeetingTitle.Trim();
        }

        // -----------------------------------------------------
        // UPDATE PARTICIPANT COUNT
        // -----------------------------------------------------

        booking.ParticipantCount =
            request.ParticipantCount;

        // -----------------------------------------------------
        // RESET REMINDER FLAGS
        // -----------------------------------------------------

        booking.StartReminderSent =
            false;

        booking.EndReminderSent =
            false;

        // -----------------------------------------------------
        // CLEAR CANCELLATION DATA
        // -----------------------------------------------------

        booking.CancellationReason =
            null;

        // -----------------------------------------------------
        // AUTO APPROVE RESCHEDULE
        // -----------------------------------------------------

        booking.Status =
            "Approved";

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
                    "Rooms can only be searched until 10:00 PM.");
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
                GetIndiaToday();

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
                    GetIndiaCurrentTime();

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

                    booking.Status != "Cancelled" &&
                    booking.Status != "Rejected" &&

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
    // GET ROOM TYPES BY MODULE
    // =========================================================

    public async Task<List<RoomTypeDto>> GetRoomTypesByModuleAsync(
        string? module,
        int? moduleId)
    {
        var query = _context.Rooms
            .AsNoTracking()
            .Include(r => r.RoomType)
            .Include(r => r.Module)
            .Where(r =>
                !r.IsBlocked &&
                r.Status != "Blocked" &&
                r.RoomType != null);

        if (!string.IsNullOrWhiteSpace(module))
        {
            var trimmedModule = module.Trim().ToLower();
            query = query.Where(r =>
                r.Module != null &&
                r.Module.ModuleName.ToLower() == trimmedModule);
        }
        else if (moduleId.HasValue && moduleId.Value > 0)
        {
            query = query.Where(r =>
                r.ModuleId == moduleId.Value);
        }

        var roomTypes = await query
            .Select(r => new RoomTypeDto
            {
                RoomTypeId = r.RoomType!.RoomTypeId,
                TypeName = r.RoomType.TypeName
            })
            .Distinct()
            .OrderBy(rt => rt.RoomTypeId)
            .ToListAsync();

        if (!roomTypes.Any() && string.IsNullOrWhiteSpace(module) && (!moduleId.HasValue || moduleId.Value <= 0))
        {
            return await _context.RoomTypes
                .AsNoTracking()
                .OrderBy(rt => rt.RoomTypeId)
                .Select(rt => new RoomTypeDto
                {
                    RoomTypeId = rt.RoomTypeId,
                    TypeName = rt.TypeName
                })
                .ToListAsync();
        }

        return roomTypes;
    }

    // =========================================================
    // GET ALL MODULES
    // =========================================================

    public async Task<List<ModuleDropdownDto>> GetModulesAsync()
    {
        return await _context.Modules
            .AsNoTracking()
            .OrderBy(m => m.ModuleId)
            .Select(m => new ModuleDropdownDto
            {
                ModuleId = m.ModuleId,
                OfficeId = m.OfficeId,
                ModuleName = m.ModuleName
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

    // =========================================================
    // GET EMPLOYEE NAME
    // =========================================================

    public async Task<string?> GetEmployeeNameAsync(
        int employeeId)
    {
        return await _context.Employees
            .AsNoTracking()
            .Where(e =>
                e.EmployeeId == employeeId)
            .Select(e =>
                e.Name)
            .FirstOrDefaultAsync();
    }

    // =========================================================
    // GET EMPLOYEE BY ID
    // =========================================================

    public async Task<Employee?> GetEmployeeByIdAsync(
        int employeeId)
    {
        return await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e =>
                e.EmployeeId == employeeId);
    }

    // =========================================================
    // GET ROOM BY ID
    // =========================================================

    public async Task<Room?> GetRoomByIdAsync(
        int roomId)
    {
        return await _context.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.RoomId == roomId);
    }

    // =========================================================
    // GET ADMIN EMAILS
    // =========================================================

    public async Task<List<string>> GetAdminEmailsAsync()
    {
        return await _context.Employees
            .AsNoTracking()
            .Include(e => e.Role)
            .Where(e =>
                e.Role != null &&

                (e.Role.RoleName == "Admin" ||
                 e.Role.RoleName == "ADMIN" ||
                 e.Role.RoleName == "admin") &&

                !string.IsNullOrWhiteSpace(e.Email))

            .Select(e =>
                e.Email)

            .ToListAsync();
    }
}