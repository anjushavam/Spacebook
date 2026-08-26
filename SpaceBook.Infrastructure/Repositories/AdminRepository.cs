using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Admin;
using SpaceBook.Application.Interfaces;
using SpaceBook.Infrastructure.Data;
using SpaceBook.Application.DTOs.Employee;

namespace SpaceBook.Infrastructure.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly ApplicationDbContext _context;
    private static readonly TimeZoneInfo IndiaTimeZone = GetIndiaTimeZone();

    public AdminRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    private static TimeZoneInfo GetIndiaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }
        catch (InvalidTimeZoneException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }
    }

    private static DateOnly GetIndiaToday()
    {
        var indiaNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IndiaTimeZone);
        return DateOnly.FromDateTime(indiaNow);
    }

    public async Task<AdminDashboardDto> GetDashboardAsync(AdminDashboardFilterDto? filter = null)
    {
        var today = GetIndiaToday();
        var (startDate, endDate) = ResolveDateRange(filter, today);

        var dashboard = new AdminDashboardDto();

        // =========================================================
        // TOTAL ROOMS / ROOM CAPACITY IN SCOPE
        // =========================================================

        var roomsQuery = _context.Rooms
            .AsNoTracking()
            .Include(r => r.Module).ThenInclude(m => m!.Office)
            .Where(r => !r.IsBlocked && r.Status != "Blocked")
            .AsQueryable();

        if (filter != null &&
            !string.IsNullOrWhiteSpace(filter.Module) &&
            !filter.Module.Equals("All", StringComparison.OrdinalIgnoreCase) &&
            !filter.Module.Equals("All Modules", StringComparison.OrdinalIgnoreCase))
        {
            var targetModule = filter.Module.Trim().ToLowerInvariant();
            roomsQuery = roomsQuery.Where(r =>
                r.Module != null &&
                (
                    r.Module.ModuleName.ToLower() == targetModule ||
                    targetModule.Contains(r.Module.ModuleName.ToLower()) ||
                    r.Module.ModuleName.ToLower().Contains(targetModule) ||
                    (r.Module.Office != null &&
                     (r.Module.ModuleName + " - " + r.Module.Office.OfficeName).ToLower() == targetModule) ||
                    (r.Module.Office != null &&
                     targetModule.Contains(r.Module.ModuleName.ToLower()) &&
                     targetModule.Contains(r.Module.Office.OfficeName.ToLower()))
                ));
        }

        if (filter?.RoomTypeId.HasValue == true && filter.RoomTypeId.Value > 0)
        {
            roomsQuery = roomsQuery.Where(r => r.RoomTypeId == filter.RoomTypeId.Value);
        }

        var totalRoomsInScope = await roomsQuery.CountAsync();
        dashboard.TotalRooms = totalRoomsInScope > 0 ? totalRoomsInScope : await _context.Rooms.CountAsync();

        // =========================================================
        // FILTERED BOOKINGS QUERY
        // =========================================================

        var bookingsQuery = _context.Bookings
            .AsNoTracking()
            .Include(b => b.Room).ThenInclude(r => r!.Module).ThenInclude(m => m!.Office)
            .Include(b => b.Employee)
            .AsQueryable();

        if (startDate.HasValue)
        {
            bookingsQuery = bookingsQuery.Where(b => b.BookingDate >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            bookingsQuery = bookingsQuery.Where(b => b.BookingDate <= endDate.Value);
        }

        if (filter?.RoomTypeId.HasValue == true && filter.RoomTypeId.Value > 0)
        {
            bookingsQuery = bookingsQuery.Where(b => b.Room != null && b.Room.RoomTypeId == filter.RoomTypeId.Value);
        }

        if (filter != null &&
            !string.IsNullOrWhiteSpace(filter.Module) &&
            !filter.Module.Equals("All", StringComparison.OrdinalIgnoreCase) &&
            !filter.Module.Equals("All Modules", StringComparison.OrdinalIgnoreCase))
        {
            var targetModule = filter.Module.Trim().ToLowerInvariant();
            bookingsQuery = bookingsQuery.Where(b =>
                b.Room != null &&
                b.Room.Module != null &&
                (
                    b.Room.Module.ModuleName.ToLower() == targetModule ||
                    targetModule.Contains(b.Room.Module.ModuleName.ToLower()) ||
                    b.Room.Module.ModuleName.ToLower().Contains(targetModule) ||
                    (b.Room.Module.Office != null &&
                     (b.Room.Module.ModuleName + " - " + b.Room.Module.Office.OfficeName).ToLower() == targetModule) ||
                    (b.Room.Module.Office != null &&
                     targetModule.Contains(b.Room.Module.ModuleName.ToLower()) &&
                     targetModule.Contains(b.Room.Module.Office.OfficeName.ToLower()))
                ));
        }

        if (filter != null &&
            !string.IsNullOrWhiteSpace(filter.Status) &&
            !filter.Status.Equals("All", StringComparison.OrdinalIgnoreCase) &&
            !filter.Status.Equals("All Status", StringComparison.OrdinalIgnoreCase) &&
            !filter.Status.Equals("All Statuses", StringComparison.OrdinalIgnoreCase))
        {
            var targetStatus = filter.Status.Trim();
            if (string.Equals(targetStatus, "Confirmed Bookings", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(targetStatus, "Confirmed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(targetStatus, "Approved", StringComparison.OrdinalIgnoreCase))
            {
                bookingsQuery = bookingsQuery.Where(b => b.Status == "Approved" || b.Status == "Confirmed");
            }
            else if (string.Equals(targetStatus, "Cancelled Bookings", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(targetStatus, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(targetStatus, "Canceled", StringComparison.OrdinalIgnoreCase))
            {
                bookingsQuery = bookingsQuery.Where(b => b.Status == "Cancelled" || b.Status == "Canceled");
            }
            else
            {
                bookingsQuery = bookingsQuery.Where(b => b.Status.ToLower() == targetStatus.ToLower());
            }
        }

        var bookings = await bookingsQuery.ToListAsync();

        dashboard.TodayBookings = bookings.Count;

        // =========================================================
        // PENDING APPROVALS
        // =========================================================

        dashboard.PendingApprovals = bookings.Count(x => x.Status == "Pending");

        // =========================================================
        // DYNAMIC UTILIZATION
        // =========================================================
        // Office hours: 10:00 AM - 10:00 PM = 12 hours
        // Total Available Room-Hours = Total Rooms in Scope * Days in Range * 12
        // Utilization = (Total Approved Booked Room-Hours / Total Available Room-Hours) * 100
        // =========================================================

        const double officeHoursPerDay = 12.0;

        int daysCount;
        if (startDate.HasValue && endDate.HasValue)
        {
            daysCount = Math.Max(1, endDate.Value.DayNumber - startDate.Value.DayNumber + 1);
        }
        else if (startDate.HasValue && !endDate.HasValue)
        {
            var maxDate = bookings.Any() ? bookings.Max(b => b.BookingDate) : today.AddDays(30);
            daysCount = Math.Max(1, maxDate.DayNumber - startDate.Value.DayNumber + 1);
        }
        else if (!startDate.HasValue && endDate.HasValue)
        {
            var minDate = bookings.Any() ? bookings.Min(b => b.BookingDate) : today.AddDays(-30);
            daysCount = Math.Max(1, endDate.Value.DayNumber - minDate.DayNumber + 1);
        }
        else
        {
            var distinctDates = bookings.Select(b => b.BookingDate).Distinct().Count();
            daysCount = Math.Max(1, distinctDates > 0 ? distinctDates : 1);
        }

        double availableRoomHours = dashboard.TotalRooms * daysCount * officeHoursPerDay;

        var approvedBookings = bookings
            .Where(x => x.Status == "Approved" || x.Status == "Confirmed")
            .ToList();

        double bookedRoomHours = approvedBookings.Sum(x =>
            Math.Max(0.5, (x.EndTime.ToTimeSpan() - x.StartTime.ToTimeSpan()).TotalHours));

        double utilization = availableRoomHours == 0
            ? 0.0
            : (bookedRoomHours / availableRoomHours) * 100.0;

        utilization = Math.Min(utilization, 100.0);

        dashboard.Utilization = Math.Round(utilization, 2);

        // =========================================================
        // PENDING APPROVAL LIST
        // =========================================================

        dashboard.PendingApprovalList = bookings
            .Where(x => x.Status == "Pending")
            .OrderBy(x => x.BookingDate)
            .Take(5)
            .Select(x => new PendingApprovalDto
            {
                BookingId = x.BookingId,
                RoomName = x.Room?.RoomName ?? $"Room #{x.RoomId}",
                BookingDate = x.BookingDate,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                RequestedBy = x.Employee?.Name ?? $"Employee #{x.EmployeeId}"
            })
            .ToList();

        // =========================================================
        // RECENT BOOKINGS
        // =========================================================

        dashboard.RecentBookings = bookings
            .OrderByDescending(x => x.BookingDate)
            .ThenByDescending(x => x.StartTime)
            .Take(10)
            .Select(x => new RecentBookingDto
            {
                RoomName = x.Room?.RoomName ?? $"Room #{x.RoomId}",
                BookingDate = x.BookingDate,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Status = x.Status
            })
            .ToList();

        // =========================================================
        // NOTIFICATIONS
        // =========================================================

        dashboard.Notifications = new List<NotificationDto>();

        return dashboard;
    }

    private static (DateOnly? Start, DateOnly? End) ResolveDateRange(AdminDashboardFilterDto? filter, DateOnly today)
    {
        if (filter == null)
        {
            return (today, today);
        }

        // 1. Explicit StartDate / EndDate takes absolute top priority
        if (filter.StartDate.HasValue || filter.EndDate.HasValue)
        {
            return (filter.StartDate ?? today, filter.EndDate ?? filter.StartDate ?? today);
        }

        var tf = filter.Timeframe?.Trim().ToLowerInvariant();

        // 2. Named Timeframes
        if (!string.IsNullOrWhiteSpace(tf) && tf != "all" && tf != "all time")
        {
            return tf switch
            {
                "daily" or "today" or "day" => (today, today),
                "yesterday" => (today.AddDays(-1), today.AddDays(-1)),
                "past 7 days" or "last 7 days" or "7 days" or "weekly" or "week" or "this week" => (today.AddDays(-6), today),
                "past 30 days" or "last 30 days" or "30 days" => (today.AddDays(-29), today),
                "this month" or "monthly" or "month" => (new DateOnly(today.Year, today.Month, 1), new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month))),
                "past dates" or "past" => (null, today.AddDays(-1)),
                "upcoming" or "future" => (today.AddDays(1), null),
                _ => (null, null)
            };
        }

        // 3. Specific Month / Year provided
        if (filter.Month.HasValue || filter.Year.HasValue)
        {
            int year = filter.Year ?? today.Year;
            int month = filter.Month ?? today.Month;
            if (month >= 1 && month <= 12)
            {
                int daysInMonth = DateTime.DaysInMonth(year, month);
                return (new DateOnly(year, month, 1), new DateOnly(year, month, daysInMonth));
            }
            else
            {
                return (new DateOnly(year, 1, 1), new DateOnly(year, 12, 31));
            }
        }

        // If module or status filter is passed with "all time", return null
        if (tf == "all" || tf == "all time")
        {
            return (null, null);
        }

        return (today, today);
    }
}