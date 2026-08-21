using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Employee;
using SpaceBook.Application.Interfaces;
using SpaceBook.Infrastructure.Data;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Infrastructure.Repositories;

public class EmployeeDashboardRepository : IEmployeeDashboardRepository
{
    private readonly ApplicationDbContext _context;

    // =========================================================
    // OFFICE HOURS
    // =========================================================
    // 10:00 AM - 07:00 PM
    // =========================================================

    private static readonly TimeOnly OfficeStartTime =
        new TimeOnly(10, 0);

    private static readonly TimeOnly OfficeEndTime =
        new TimeOnly(19, 0);

    public EmployeeDashboardRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // EMPLOYEE DASHBOARD
    // =========================================================

    public async Task<EmployeeDashboardDto> GetDashboardAsync(
        int employeeId)
    {
        if (employeeId <= 0)
        {
            throw new ArgumentException(
                "Invalid employee ID.");
        }

        var now = DateTime.Now;

        var today =
            DateOnly.FromDateTime(now);

        var currentTime =
            TimeOnly.FromDateTime(now);

        // =====================================================
        // BOOKINGS TODAY
        // =====================================================

        var bookingsToday =
            await _context.Bookings
                .AsNoTracking()
                .CountAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.BookingDate == today &&
                    x.Status != "Cancelled" &&
                    x.Status != "Rejected");

        // =====================================================
        // UPCOMING BOOKINGS
        // =====================================================

        var upcomingCount =
            await _context.Bookings
                .AsNoTracking()
                .CountAsync(x =>
                    x.EmployeeId == employeeId &&
                    (
                        x.BookingDate > today ||
                        (
                            x.BookingDate == today &&
                            x.EndTime > currentTime
                        )
                    ) &&
                    x.Status != "Cancelled" &&
                    x.Status != "Rejected");

        // =====================================================
        // RECENT RESERVATIONS
        // =====================================================

        var recentReservations =
            await GetRecentReservationsAsync(employeeId);

        // =====================================================
        // TODAY'S MEETINGS
        // =====================================================

        var todayMeetings =
            await _context.Bookings
                .AsNoTracking()
                .Include(x => x.Room)
                    .ThenInclude(r => r!.Module)
                .Where(x =>
                    x.EmployeeId == employeeId &&
                    x.BookingDate == today &&
                    x.Status != "Cancelled" &&
                    x.Status != "Rejected")
                .OrderBy(x => x.StartTime)
                .Select(x => new TodayMeetingDto
                {
                    BookingId =
                        x.BookingId,

                    Purpose =
                        !string.IsNullOrWhiteSpace(x.Purpose)
                            ? x.Purpose
                            : (
                                !string.IsNullOrWhiteSpace(
                                    x.MeetingTitle)
                                    ? x.MeetingTitle
                                    : "Reserved Workspace"
                              ),

                    RoomName =
                        x.Room != null
                            ? x.Room.RoomName
                            : $"Room {x.RoomId}",

                    Module =
                        x.Room != null &&
                        x.Room.Module != null
                            ? x.Room.Module.ModuleName
                            : string.Empty,

                    StartTime =
                        x.StartTime,

                    EndTime =
                        x.EndTime,

                    Status =
                        x.Status
                })
                .ToListAsync();

        // =====================================================
        // RETURN DASHBOARD
        // =====================================================

        return new EmployeeDashboardDto
        {
            BookingsToday =
                bookingsToday,

            UpcomingCount =
                upcomingCount,

            RecentReservations =
                recentReservations
                    .Take(5)
                    .ToList(),

            TodayMeetings =
                todayMeetings
        };
    }

    // =========================================================
    // EMPLOYEE AVAILABILITY
    // =========================================================

    public async Task<AvailabilityCalendarDto> GetAvailabilityAsync(
        DateOnly date,
        int? roomTypeId)
    {
        var now =
            DateTime.Now;

        var today =
            DateOnly.FromDateTime(now);

        var currentTime =
            TimeOnly.FromDateTime(now);

        // =====================================================
        // CREATE RESULT
        // =====================================================

        var result =
            new AvailabilityCalendarDto
            {
                Date = date,

                Rooms =
                    new List<RoomAvailabilityDto>()
            };

        // =====================================================
        // PAST DATE
        // =====================================================

        if (date < today)
        {
            return result;
        }

        // =====================================================
        // WEEKEND
        // =====================================================

        if (date.DayOfWeek == DayOfWeek.Saturday ||
            date.DayOfWeek == DayOfWeek.Sunday)
        {
            return result;
        }

        // =====================================================
        // ROOM QUERY
        // =====================================================

        var roomsQuery =
            _context.Rooms
                .AsNoTracking()
                .Where(r =>
                    !r.IsBlocked &&
                    r.Status != "Blocked");

        // =====================================================
        // ROOM TYPE FILTER
        // =====================================================

        if (roomTypeId.HasValue)
        {
            roomsQuery =
                roomsQuery.Where(r =>
                    r.RoomTypeId ==
                    roomTypeId.Value);
        }

        // =====================================================
        // LOAD ROOMS
        // =====================================================
        //
        // IMPORTANT:
        //
        // Room
        //   -> RoomFacilities
        //       -> Facility
        //
        // The ThenInclude is required so FacilityName is
        // available when building the response.
        // =====================================================

        var rooms =
            await roomsQuery
                .Include(r => r.RoomType)
                .Include(r => r.Module)
                .Include(r => r.RoomFacilities)
                    .ThenInclude(rf => rf.Facility)
                .ToListAsync();

        // =====================================================
        // ROOM IDS
        // =====================================================

        var roomIds =
            rooms
                .Select(r => r.RoomId)
                .ToList();

        // =====================================================
        // GET BOOKINGS
        // =====================================================

        var allBookings =
            roomIds.Count == 0
                ? new List<Booking>()
                : await _context.Bookings
                    .AsNoTracking()
                    .Where(b =>
                        roomIds.Contains(b.RoomId) &&
                        b.BookingDate == date &&
                        b.Status != "Cancelled" &&
                        b.Status != "Rejected")
                    .ToListAsync();

        // =====================================================
        // PROCESS ROOMS
        // =====================================================

        foreach (var room in rooms)
        {
            // =================================================
            // ROOM BOOKINGS
            // =================================================

            var bookings =
                allBookings
                    .Where(b =>
                        b.RoomId ==
                        room.RoomId)
                    .ToList();

            // =================================================
            // TIME SLOTS
            // =================================================

            var slots =
                new List<TimeSlotDto>();

            if (date > today)
            {
                GenerateTimeSlots(
                    slots,
                    bookings,
                    OfficeStartTime,
                    OfficeEndTime);
            }
            else
            {
                GenerateTodayTimeSlots(
                    slots,
                    bookings,
                    OfficeStartTime,
                    OfficeEndTime,
                    currentTime);
            }

            // =================================================
            // CURRENT / NEXT BOOKING
            // =================================================

            Booking? currentBooking = null;

            // =================================================
            // CURRENT BOOKING
            // =================================================

            if (date == today)
            {
                currentBooking =
                    bookings
                        .Where(b =>
                            b.StartTime <= currentTime &&
                            b.EndTime > currentTime)
                        .OrderBy(b =>
                            b.StartTime)
                        .FirstOrDefault();
            }

            // =================================================
            // NEXT BOOKING
            // =================================================

            if (currentBooking == null &&
                date >= today)
            {
                if (date == today)
                {
                    currentBooking =
                        bookings
                            .Where(b =>
                                b.StartTime > currentTime)
                            .OrderBy(b =>
                                b.StartTime)
                            .FirstOrDefault();
                }
                else
                {
                    currentBooking =
                        bookings
                            .OrderBy(b =>
                                b.StartTime)
                            .FirstOrDefault();
                }
            }

            // =================================================
            // FACILITIES
            // =================================================
            //
            // Room
            //   -> RoomFacilities
            //       -> Facility
            //
            // Example:
            //
            // Room 1
            //   RoomFacilities:
            //      FacilityId = 1 -> Projector
            //      FacilityId = 2 -> Whiteboard
            //      FacilityId = 3 -> WiFi
            //
            // Response:
            //
            // "facilities": [
            //     "Projector",
            //     "Whiteboard",
            //     "WiFi"
            // ]
            // =================================================

            var facilities =
                room.RoomFacilities?
                    .Where(rf =>
                        rf.Facility != null)
                    .Select(rf =>
                        rf.Facility!.FacilityName)
                    .Where(name =>
                        !string.IsNullOrWhiteSpace(name))
                    .Select(name =>
                        name.Trim())
                    .Distinct()
                    .ToList()
                ?? new List<string>();

            // =================================================
            // ROOM STATUS
            // =================================================

            string roomStatus =
                "Available";

            if (date == today)
            {
                var activeBooking =
                    bookings.FirstOrDefault(b =>
                        b.StartTime <= currentTime &&
                        b.EndTime > currentTime);

                if (activeBooking != null)
                {
                    roomStatus =
                        "Booked";
                }
            }
            else if (date > today)
            {
                if (bookings.Any())
                {
                    roomStatus =
                        "Booked";
                }
            }

            // =================================================
            // ADD ROOM
            // =================================================

            result.Rooms.Add(
                new RoomAvailabilityDto
                {
                    RoomId =
                        room.RoomId,

                    RoomName =
                        room.RoomName,

                    RoomType =
                        room.RoomType != null
                            ? room.RoomType.TypeName
                            : "Conference",

                    Module =
                        room.Module != null
                            ? room.Module.ModuleName
                            : string.Empty,

                    Capacity =
                        room.Capacity,

                    // =========================================
                    // FACILITIES
                    // =========================================

                    Facilities =
                        facilities,

                    Status =
                        roomStatus,

                    AvailableSlots =
                        slots.Count(x =>
                            !x.IsBooked),

                    TimeSlots =
                        slots,

                    // =========================================
                    // CURRENT BOOKING
                    // =========================================

                    CurrentBooking =
                        currentBooking == null
                            ? null
                            : new BookingPreviewDto
                            {
                                Purpose =
                                    !string.IsNullOrWhiteSpace(
                                        currentBooking.Purpose)
                                        ? currentBooking.Purpose
                                        : (
                                            !string.IsNullOrWhiteSpace(
                                                currentBooking.MeetingTitle)
                                                    ? currentBooking.MeetingTitle
                                                    : "Reserved Workspace"
                                          ),

                                StartTime =
                                    currentBooking.StartTime,

                                EndTime =
                                    currentBooking.EndTime,

                                Status =
                                    currentBooking.Status
                            }
                });
        }

        // =====================================================
        // RETURN
        // =====================================================

        return result;
    }

    // =========================================================
    // GENERATE FUTURE TIME SLOTS
    // =========================================================

    private static void GenerateTimeSlots(
        List<TimeSlotDto> slots,
        List<Booking> bookings,
        TimeOnly officeStart,
        TimeOnly officeEnd)
    {
        var start =
            officeStart;

        while (start < officeEnd)
        {
            var end =
                start.AddHours(1);

            if (end > officeEnd)
            {
                end =
                    officeEnd;
            }

            if (end <= start)
            {
                break;
            }

            var isBooked =
                bookings.Any(b =>
                    b.StartTime < end &&
                    b.EndTime > start);

            slots.Add(
                new TimeSlotDto
                {
                    StartTime =
                        start,

                    EndTime =
                        end,

                    IsBooked =
                        isBooked
                });

            start =
                end;
        }
    }

    // =========================================================
    // GENERATE TODAY'S TIME SLOTS
    // =========================================================

    private static void GenerateTodayTimeSlots(
        List<TimeSlotDto> slots,
        List<Booking> bookings,
        TimeOnly officeStart,
        TimeOnly officeEnd,
        TimeOnly currentTime)
    {
        var start =
            officeStart;

        while (start < officeEnd)
        {
            var end =
                start.AddHours(1);

            if (end > officeEnd)
            {
                end =
                    officeEnd;
            }

            if (end <= start)
            {
                break;
            }

            // -----------------------------------------------
            // Skip completely passed slots
            // -----------------------------------------------

            if (end <= currentTime)
            {
                start =
                    end;

                continue;
            }

            // -----------------------------------------------
            // BOOKING CHECK
            // -----------------------------------------------

            var isBooked =
                bookings.Any(b =>
                    b.StartTime < end &&
                    b.EndTime > start);

            slots.Add(
                new TimeSlotDto
                {
                    StartTime =
                        start,

                    EndTime =
                        end,

                    IsBooked =
                        isBooked
                });

            start =
                end;
        }
    }

    // =========================================================
    // MY BOOKINGS
    // =========================================================

    public async Task<List<MyBookingDto>> GetMyBookingsAsync(
        int employeeId)
    {
        if (employeeId <= 0)
        {
            throw new ArgumentException(
                "Invalid employee ID.");
        }

        return await _context.Bookings
            .AsNoTracking()

            .Include(x => x.Room)
                .ThenInclude(r => r!.Module)

            .Where(x =>
                x.EmployeeId ==
                employeeId)

            .OrderByDescending(x =>
                x.BookingDate)

            .ThenByDescending(x =>
                x.StartTime)

            .Select(x => new MyBookingDto
            {
                BookingId =
                    x.BookingId,

                RoomId =
                    x.RoomId,

                RoomName =
                    x.Room != null
                        ? x.Room.RoomName
                        : $"Room {x.RoomId}",

                Module =
                    x.Room != null &&
                    x.Room.Module != null
                        ? x.Room.Module.ModuleName
                        : string.Empty,

                Purpose =
                    !string.IsNullOrWhiteSpace(
                        x.Purpose)
                        ? x.Purpose
                        : (
                            !string.IsNullOrWhiteSpace(
                                x.MeetingTitle)
                                    ? x.MeetingTitle
                                    : "Reserved Workspace"
                          ),

                BookingDate =
                    x.BookingDate,

                StartTime =
                    x.StartTime,

                EndTime =
                    x.EndTime,

                Status =
                    x.Status
            })
            .ToListAsync();
    }

    // =========================================================
    // RECENT RESERVATIONS
    // =========================================================

    public async Task<List<RecentReservationDto>>
        GetRecentReservationsAsync(
            int employeeId)
    {
        if (employeeId <= 0)
        {
            throw new ArgumentException(
                "Invalid employee ID.");
        }

        return await _context.Bookings
            .AsNoTracking()

            .Include(x => x.Room)
                .ThenInclude(r => r!.Module)

            .Where(x =>
                x.EmployeeId == employeeId &&
                x.Status != "Cancelled" &&
                x.Status != "Rejected")

            .OrderByDescending(x =>
                x.BookingDate)

            .ThenByDescending(x =>
                x.StartTime)

            .Select(x => new RecentReservationDto
            {
                BookingId =
                    x.BookingId,

                RoomName =
                    x.Room != null
                        ? x.Room.RoomName
                        : $"Room {x.RoomId}",

                Module =
                    x.Room != null &&
                    x.Room.Module != null
                        ? x.Room.Module.ModuleName
                        : string.Empty,

                BookingDate =
                    x.BookingDate,

                StartTime =
                    x.StartTime,

                EndTime =
                    x.EndTime,

                Status =
                    x.Status
            })
            .ToListAsync();
    }
}