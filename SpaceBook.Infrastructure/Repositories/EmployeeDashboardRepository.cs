using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Employee;
using SpaceBook.Application.Interfaces;
using SpaceBook.Infrastructure.Data;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Infrastructure.Repositories;

public class EmployeeDashboardRepository : IEmployeeDashboardRepository
{
    private readonly ApplicationDbContext _context;

    public EmployeeDashboardRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // OFFICE HOURS
    // =========================================================
    // Office Hours:
    // 10:00 AM - 07:30 PM
    // =========================================================

    private static readonly TimeOnly OfficeStartTime =
        new TimeOnly(10, 0);

    private static readonly TimeOnly OfficeEndTime =
        new TimeOnly(19, 30);


    // =========================================================
    // EMPLOYEE DASHBOARD
    // =========================================================

    public async Task<EmployeeDashboardDto> GetDashboardAsync(
        int employeeId)
    {
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

                // Load Room
                .Include(x => x.Room)

                // Load Module through Room
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
                                !string.IsNullOrWhiteSpace(x.MeetingTitle)
                                    ? x.MeetingTitle
                                    : "Reserved Workspace"
                              ),

                    RoomName =
                        x.Room != null
                            ? x.Room.RoomName
                            : $"Room {x.RoomId}",

                    // =================================================
                    // MODULE NAME
                    // =================================================
                    // rooms.moduleid
                    //        ↓
                    // modules.moduleid
                    //        ↓
                    // modules.modulename
                    // =================================================

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


        var result =
            new AvailabilityCalendarDto
            {
                Date = date
            };


        TimeOnly officeStart =
            OfficeStartTime;

        TimeOnly officeEnd =
            OfficeEndTime;


        // =====================================================
        // GET ROOMS
        // =====================================================

        var roomsQuery =
            _context.Rooms
                .AsNoTracking()
                .Where(r =>
                    !r.IsBlocked &&
                    r.Status != "Blocked")
                .AsQueryable();


        // =====================================================
        // FILTER BY ROOM TYPE
        // =====================================================

        if (roomTypeId.HasValue)
        {
            roomsQuery =
                roomsQuery.Where(r =>
                    r.RoomTypeId ==
                    roomTypeId.Value);
        }


        // =====================================================
        // LOAD ROOM DATA
        // =====================================================
        // Room
        //   ├── RoomType
        //   ├── Module
        //   └── RoomFacilities
        //          └── Facility
        // =====================================================

        var rooms =
            await roomsQuery

                .Include(r => r.RoomType)

                .Include(r => r.Module)

                .Include(r => r.RoomFacilities)
                    .ThenInclude(rf => rf.Facility)

                .ToListAsync();


        // =====================================================
        // GET ROOM IDS
        // =====================================================

        var roomIds =
            rooms
                .Select(r => r.RoomId)
                .ToList();


        // =====================================================
        // GET BOOKINGS FOR SELECTED DATE
        // =====================================================

        var allBookings =
            await _context.Bookings
                .AsNoTracking()
                .Where(b =>
                    roomIds.Contains(b.RoomId) &&
                    b.BookingDate == date &&
                    b.Status != "Cancelled" &&
                    b.Status != "Rejected")
                .ToListAsync();


        // =====================================================
        // PROCESS EACH ROOM
        // =====================================================

        foreach (var room in rooms)
        {
            var bookings =
                allBookings
                    .Where(b =>
                        b.RoomId == room.RoomId)
                    .ToList();


            List<TimeSlotDto> slots =
                new();


            // =================================================
            // PAST DATE
            // =================================================

            if (date < today)
            {
                slots =
                    new List<TimeSlotDto>();
            }


            // =================================================
            // FUTURE DATE
            // =================================================

            else if (date > today)
            {
                TimeOnly start =
                    officeStart;


                while (start < officeEnd)
                {
                    TimeOnly end =
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


                    bool isBooked =
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


            // =================================================
            // TODAY
            // =================================================

            else
            {
                TimeOnly start =
                    officeStart;


                while (start < officeEnd)
                {
                    TimeOnly end =
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


                    // Skip slots that have already passed

                    if (end <= currentTime)
                    {
                        start =
                            end;

                        continue;
                    }


                    bool isBooked =
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


            // =================================================
            // CURRENT / NEXT BOOKING
            // =================================================

            Booking? currentBooking =
                null;


            // -------------------------------------------------
            // CURRENT BOOKING
            // -------------------------------------------------

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


            // -------------------------------------------------
            // NEXT BOOKING
            // -------------------------------------------------

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

            var facilities =
                room.RoomFacilities != null
                    ? room.RoomFacilities
                        .Where(rf =>
                            rf.Facility != null)
                        .Select(rf =>
                            rf.Facility!.FacilityName)
                        .ToList()
                    : new List<string>();


            // =================================================
            // ROOM STATUS
            // =================================================

            string roomStatus =
                "Available";


            // -------------------------------------------------
            // TODAY
            // -------------------------------------------------

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


            // -------------------------------------------------
            // FUTURE DATE
            // -------------------------------------------------

            else if (date > today)
            {
                if (bookings.Any())
                {
                    roomStatus =
                        "Booked";
                }
            }


            // =================================================
            // ADD ROOM AVAILABILITY
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


                    // =================================================
                    // MODULE NAME
                    // =================================================
                    // Do NOT use:
                    //
                    // room.Module
                    //
                    // because room.Module is a Module entity.
                    //
                    // Use:
                    //
                    // room.Module.ModuleName
                    // =================================================

                    Module =
                        room.Module != null
                            ? room.Module.ModuleName
                            : string.Empty,


                    Capacity =
                        room.Capacity,


                    Facilities =
                        facilities,


                    Status =
                        roomStatus,


                    AvailableSlots =
                        slots.Count(x =>
                            !x.IsBooked),


                    TimeSlots =
                        slots,


                    // =================================================
                    // CURRENT / NEXT BOOKING
                    // =================================================

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


        return result;
    }


    // =========================================================
    // MY BOOKINGS
    // =========================================================

    public async Task<List<MyBookingDto>> GetMyBookingsAsync(
        int employeeId)
    {
        return await _context.Bookings
            .AsNoTracking()

            // Load Room
            .Include(x => x.Room)

            // Load Module
            .ThenInclude(r => r!.Module)

            .Where(x =>
                x.EmployeeId == employeeId)

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


                // =================================================
                // MODULE NAME
                // =================================================

                Module =
                    x.Room != null &&
                    x.Room.Module != null
                        ? x.Room.Module.ModuleName
                        : string.Empty,


                Purpose =
                    !string.IsNullOrWhiteSpace(x.Purpose)
                        ? x.Purpose
                        : (
                            !string.IsNullOrWhiteSpace(x.MeetingTitle)
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
        return await _context.Bookings
            .AsNoTracking()

            // Load Room
            .Include(x => x.Room)

            // Load Module
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


                // =================================================
                // MODULE NAME
                // =================================================

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