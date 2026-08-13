using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Employee;
using SpaceBook.Application.Interfaces;
using SpaceBook.Infrastructure.Data;
using SpaceBook.Application.DTOs.Admin;

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
        var today = DateOnly.FromDateTime(DateTime.Today);
        var currentTime = TimeOnly.FromDateTime(DateTime.Now);

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
        var today =
            DateOnly.FromDateTime(DateTime.Now);

        var currentTime =
            TimeOnly.FromDateTime(DateTime.Now);

        var result = new AvailabilityCalendarDto
        {
            Date = date
        };


        // =====================================================
        // OFFICE HOURS
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


            TimeOnly start = officeStart;


            // =================================================
            // FUTURE DATE
            // Show all slots from 09:00 AM to 07:30 PM
            // =================================================

            if (date > today)
            {
                while (start < officeEnd)
                {
                    TimeOnly end =
                        start.AddHours(1);


                    // Last slot:
                    // 07:00 PM - 07:30 PM

                    if (end > officeEnd)
                    {
                        end = officeEnd;
                    }


                    bool isBooked =
                        bookings.Any(b =>
                            b.StartTime < end &&
                            b.EndTime > start);


                    slots.Add(new TimeSlotDto
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
            // Only show slots that have NOT already ended
            // =================================================

            else if (date == today)
            {
                while (start < officeEnd)
                {
                    TimeOnly end =
                        start.AddHours(1);


                    // Last slot:
                    // 07:00 PM - 07:30 PM

                    if (end > officeEnd)
                    {
                        end = officeEnd;
                    }


                    // -----------------------------------------
                    // IMPORTANT
                    // Skip slots that have already ended
                    //
                    // Example:
                    // Current time = 04:00 PM
                    //
                    // 09-10  -> hidden
                    // 10-11  -> hidden
                    // 11-12  -> hidden
                    // 12-01  -> hidden
                    // 01-02  -> hidden
                    // 02-03  -> hidden
                    // 03-04  -> hidden
                    // 04-05  -> shown
                    // 05-06  -> shown
                    // 06-07  -> shown
                    // 07-07:30 -> shown
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


                    slots.Add(new TimeSlotDto
                    {
                        StartTime = start,
                        EndTime = end,
                        IsBooked = isBooked
                    });


                    start = end;
                }
            }


            // =================================================
            // PAST DATE
            // No available slots
            // =================================================

            else
            {
                slots = new List<TimeSlotDto>();
            }


            // =================================================
            // CURRENT / UPCOMING BOOKING
            // =================================================

            var currentBooking =
                bookings
                    .Where(b =>
                        b.StartTime <= currentTime &&
                        b.EndTime > currentTime)
                    .OrderBy(b => b.StartTime)
                    .FirstOrDefault();


            // =================================================
            // IF THERE IS NO CURRENT BOOKING,
            // GET NEXT UPCOMING BOOKING
            // =================================================

            if (currentBooking == null)
            {
                currentBooking =
                    bookings
                        .Where(b =>
                            b.StartTime > currentTime)
                        .OrderBy(b => b.StartTime)
                        .FirstOrDefault();
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
            else if (date > today)
            {
                if (bookings.Any())
                {
                    roomStatus = "Booked";
                }
            }


            // =================================================
            // ADD ROOM TO RESPONSE
            // =================================================

            result.Rooms.Add(
                new RoomAvailabilityDto
                {
                    RoomId = room.RoomId,

                    RoomName = room.RoomName,

                    RoomType =
                        room.RoomType != null
                            ? room.RoomType.TypeName
                            : "Conference",

                    Module = room.Module,

                    Capacity = room.Capacity,

                    Facilities = facilities,

                    Status = roomStatus,

                    AvailableSlots =
                        slots.Count(x => !x.IsBooked),

                    TimeSlots = slots,

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
                BookingId = x.BookingId,

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

                BookingDate = x.BookingDate,

                StartTime = x.StartTime,

                EndTime = x.EndTime,

                Status = x.Status
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
                BookingId = x.BookingId,

                RoomName =
                    x.Room != null
                        ? x.Room.RoomName
                        : $"Room {x.RoomId}",

                BookingDate = x.BookingDate,

                StartTime = x.StartTime,

                EndTime = x.EndTime,

                Status = x.Status
            })
            .ToListAsync();
    }
}