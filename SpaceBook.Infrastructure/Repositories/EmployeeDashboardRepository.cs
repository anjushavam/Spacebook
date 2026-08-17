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
    // Configured Office Hours:
    // 10:00 AM - 07:00 PM
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

        var bookingsToday =
            await _context.Bookings
                .AsNoTracking()
                .CountAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.BookingDate == today &&
                    x.Status != "Cancelled" &&
                    x.Status != "Rejected");

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

        var recentReservations =
            await GetRecentReservationsAsync(employeeId);

        var todayMeetings =
            await _context.Bookings
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

                    StartTime =
                        x.StartTime,

                    EndTime =
                        x.EndTime,

                    Status =
                        x.Status
                })
                .ToListAsync();

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
    // EMPLOYEE AVAILABILITY CALENDAR
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

        var roomsQuery =
            _context.Rooms
                .AsNoTracking()
                .Where(r =>
                    !r.IsBlocked &&
                    r.Status != "Blocked")
                .AsQueryable();

        if (roomTypeId.HasValue)
        {
            roomsQuery =
                roomsQuery.Where(r =>
                    r.RoomTypeId ==
                    roomTypeId.Value);
        }

        var rooms =
            await roomsQuery
                .Include(r => r.RoomType)
                .Include(r => r.RoomFacilities)
                    .ThenInclude(rf => rf.Facility)
                .ToListAsync();

        var roomIds =
            rooms
                .Select(r => r.RoomId)
                .ToList();

        var allBookings =
            await _context.Bookings
                .AsNoTracking()
                .Where(b =>
                    roomIds.Contains(b.RoomId) &&
                    b.BookingDate == date &&
                    b.Status != "Cancelled" &&
                    b.Status != "Rejected")
                .ToListAsync();

        foreach (var room in rooms)
        {
            var bookings =
                allBookings
                    .Where(b =>
                        b.RoomId == room.RoomId)
                    .ToList();

            List<TimeSlotDto> slots =
                new();

            if (date < today)
            {
                slots =
                    new List<TimeSlotDto>();
            }
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

            Booking? currentBooking =
                null;

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

            var facilities =
                room.RoomFacilities != null
                    ? room.RoomFacilities
                        .Where(rf =>
                            rf.Facility != null)
                        .Select(rf =>
                            rf.Facility!.FacilityName)
                        .ToList()
                    : new List<string>();

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
                        slots.Count(x =>
                            !x.IsBooked),

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
                    x.Room != null
                        ? x.Room.Module
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
            .Include(x => x.Room)
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
