using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Employee;
using SpaceBook.Application.Interfaces;
using SpaceBook.Infrastructure.Data;
using SpaceBook.Application.DTOs.Admin;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Infrastructure.Repositories;

public class EmployeeDashboardRepository : IEmployeeDashboardRepository
{
    private readonly ApplicationDbContext _context;

    public EmployeeDashboardRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // EMPLOYEE DASHBOARD
    // =========================================================

    public async Task<EmployeeDashboardDto> GetDashboardAsync(int employeeId)
    {
        var now = DateTime.Now;

        var today = DateOnly.FromDateTime(now);
        var currentTime = TimeOnly.FromDateTime(now);

        var bookingsToday = await _context.Bookings
            .AsNoTracking()
            .CountAsync(x =>
                x.EmployeeId == employeeId &&
                x.BookingDate == today &&
                x.Status != "Cancelled" &&
                x.Status != "Rejected");

        var upcomingCount = await _context.Bookings
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

        var recentReservations =
            await GetRecentReservationsAsync(employeeId);

        var todayMeetings = await _context.Bookings
            .AsNoTracking()
            .Include(x => x.Room)
            .Where(x =>
                x.EmployeeId == employeeId &&
                x.BookingDate == today &&
                x.Status != "Cancelled" &&
                x.Status != "Rejected")
            .OrderBy(x => x.StartTime)
            .Select(x => new TodayMeetingDto
            {
                BookingId = x.BookingId,

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

                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Status = x.Status
            })
            .ToListAsync();

        return new EmployeeDashboardDto
        {
            BookingsToday = bookingsToday,

            UpcomingCount = upcomingCount,

            RecentReservations =
                recentReservations
                    .Take(5)
                    .ToList(),

            TodayMeetings = todayMeetings
        };
    }


    // =========================================================
    // EMPLOYEE AVAILABILITY CALENDAR
    // =========================================================

    public async Task<AvailabilityCalendarDto> GetAvailabilityAsync(
        DateOnly date,
        int? roomTypeId)
    {
        // =====================================================
        // CURRENT DATE AND TIME
        // =====================================================

        var now = DateTime.Now;

        var today =
            DateOnly.FromDateTime(now);

        var currentTime =
            TimeOnly.FromDateTime(now);


        // =====================================================
        // RESULT
        // =====================================================

        var result = new AvailabilityCalendarDto
        {
            Date = date
        };


        // =====================================================
        // OFFICE HOURS
        //
        // 09:00 AM -> 07:30 PM
        // =====================================================

        TimeOnly officeStart =
            new TimeOnly(9, 0);

        TimeOnly officeEnd =
            new TimeOnly(19, 30);


        // =====================================================
        // LOAD ROOMS
        // =====================================================

        var roomsQuery = _context.Rooms
            .Include(r => r.RoomType)
            .Include(r => r.RoomFacilities)
                .ThenInclude(rf => rf.Facility)
            .AsNoTracking()
            .AsQueryable();


        // =====================================================
        // FILTER BY ROOM TYPE
        // =====================================================

        if (roomTypeId.HasValue)
        {
            roomsQuery = roomsQuery
                .Where(r =>
                    r.RoomTypeId == roomTypeId.Value);
        }


        var rooms =
            await roomsQuery.ToListAsync();


        // =====================================================
        // LOOP THROUGH ROOMS
        // =====================================================

        foreach (var room in rooms)
        {
            // =================================================
            // GET BOOKINGS FOR SELECTED DATE
            // =================================================

            var bookings =
                await _context.Bookings
                    .AsNoTracking()
                    .Where(b =>
                        b.RoomId == room.RoomId &&
                        b.BookingDate == date &&
                        b.Status != "Cancelled" &&
                        b.Status != "Rejected")
                    .ToListAsync();


            // =================================================
            // CREATE TIME SLOTS
            // =================================================

            List<TimeSlotDto> slots = new();


            // =================================================
            // PAST DATE
            // =================================================

            if (date < today)
            {
                slots = new List<TimeSlotDto>();
            }


            // =================================================
            // FUTURE DATE
            //
            // Show:
            //
            // 09:00 - 10:00
            // 10:00 - 11:00
            // ...
            // 18:00 - 19:00
            // 19:00 - 19:30
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
                        end = officeEnd;
                    }

                    bool isBooked =
                        bookings.Any(b =>
                            b.StartTime < end &&
                            b.EndTime > start);

                    slots.Add(
                        new TimeSlotDto
                        {
                            StartTime = start,
                            EndTime = end,
                            IsBooked = isBooked
                        });

                    start = end;
                }
            }


            // =================================================
            // TODAY
            //
            // IMPORTANT:
            // Never show a slot that has already ended.
            //
            // Example:
            // Current time = 03:45 PM
            //
            // 09-10 -> hidden
            // 10-11 -> hidden
            // 11-12 -> hidden
            // 12-01 -> hidden
            // 01-02 -> hidden
            // 02-03 -> hidden
            // 03-04 -> shown
            // 04-05 -> shown
            // ...
            // 07-07:30 -> shown
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
                        end = officeEnd;
                    }


                    // -----------------------------------------
                    // DO NOT SHOW COMPLETELY PASSED SLOT
                    // -----------------------------------------

                    if (end <= currentTime)
                    {
                        start = end;
                        continue;
                    }


                    bool isBooked =
                        bookings.Any(b =>
                            b.StartTime < end &&
                            b.EndTime > start);


                    slots.Add(
                        new TimeSlotDto
                        {
                            StartTime = start,
                            EndTime = end,
                            IsBooked = isBooked
                        });


                    start = end;
                }
            }


            // =================================================
            // CURRENT BOOKING
            // =================================================

            Booking? currentBooking = null;

            if (date == today)
            {
                currentBooking =
                    bookings
                        .Where(b =>
                            b.StartTime <= currentTime &&
                            b.EndTime > currentTime)
                        .OrderBy(b => b.StartTime)
                        .FirstOrDefault();
            }


            // =================================================
            // NEXT UPCOMING BOOKING
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
                            .OrderBy(b => b.StartTime)
                            .FirstOrDefault();
                }
                else
                {
                    currentBooking =
                        bookings
                            .OrderBy(b => b.StartTime)
                            .FirstOrDefault();
                }
            }


            // =================================================
            // GET FACILITIES
            // =================================================

            var facilities =
                room.RoomFacilities
                    .Where(rf =>
                        rf.Facility != null)
                    .Select(rf =>
                        rf.Facility!.FacilityName)
                    .ToList();


            // =================================================
            // ROOM STATUS
            // =================================================

            string roomStatus = "Available";


            // TODAY
            if (date == today)
            {
                var activeBooking =
                    bookings.FirstOrDefault(b =>
                        b.StartTime <= currentTime &&
                        b.EndTime > currentTime);

                if (activeBooking != null)
                {
                    roomStatus = "Booked";
                }
            }


            // FUTURE DATE
            else if (date > today)
            {
                if (bookings.Any())
                {
                    roomStatus = "Booked";
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
                        room.Module,

                    Capacity =
                        room.Capacity,

                    Facilities =
                        facilities,

                    Status =
                        roomStatus,

                    AvailableSlots =
                        slots.Count(x => !x.IsBooked),

                    TimeSlots =
                        slots,

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
            .Include(x => x.Room)
            .Where(x =>
                x.EmployeeId == employeeId)
            .OrderByDescending(x => x.BookingDate)
            .ThenByDescending(x => x.StartTime)
            .Select(x => new MyBookingDto
            {
                BookingId =
                    x.BookingId,

                RoomName =
                    x.Room != null
                        ? x.Room.RoomName
                        : $"Room {x.RoomId}",

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
            .Include(x => x.Room)
            .Where(x =>
                x.EmployeeId == employeeId &&
                x.Status != "Cancelled" &&
                x.Status != "Rejected")
            .OrderByDescending(x => x.BookingDate)
            .ThenByDescending(x => x.StartTime)
            .Select(x => new RecentReservationDto
            {
                BookingId =
                    x.BookingId,

                RoomName =
                    x.Room != null
                        ? x.Room.RoomName
                        : $"Room {x.RoomId}",

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