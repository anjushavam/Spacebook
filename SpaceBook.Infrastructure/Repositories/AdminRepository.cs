using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Admin;
using SpaceBook.Application.Interfaces;
using SpaceBook.Infrastructure.Data;
using SpaceBook.Application.DTOs.Employee;

namespace SpaceBook.Infrastructure.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly ApplicationDbContext _context;

    public AdminRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminDashboardDto> GetDashboardAsync()
    {
        // =========================================================
        // TODAY
        // =========================================================

        var today = DateOnly.FromDateTime(DateTime.Today);

        var dashboard = new AdminDashboardDto();

        // =========================================================
        // TOTAL ROOMS
        // =========================================================

        dashboard.TotalRooms = await _context.Rooms.CountAsync();

        // =========================================================
        // TODAY'S BOOKINGS
        // =========================================================

        dashboard.TodayBookings = await _context.Bookings
            .CountAsync(x => x.BookingDate == today);

        // =========================================================
        // PENDING APPROVALS
        // =========================================================

        dashboard.PendingApprovals = await _context.Bookings
            .CountAsync(x => x.Status == "Pending");

        // =========================================================
        // UTILIZATION
        // =========================================================
        //
        // Office hours:
        // 10:00 AM - 10:00 PM = 12 hours
        //
        // Formula:
        //
        // Utilization =
        // (Total Approved Booked Room-Hours
        //  / Total Available Room-Hours) * 100
        //
        // Total Available Room-Hours =
        // Total Rooms * 12 hours
        //
        // Cancelled, Rejected and Pending bookings
        // are NOT included in utilization.
        // =========================================================

        const double officeHoursPerDay = 12.0;

        // Get today's approved bookings.
        var approvedBookings = await _context.Bookings
            .Where(x =>
                x.BookingDate == today &&
                x.Status == "Approved")
            .Select(x => new
            {
                x.StartTime,
                x.EndTime
            })
            .ToListAsync();

        // Calculate total booked room-hours.
        double bookedRoomHours = approvedBookings.Sum(x =>
            (x.EndTime.ToTimeSpan() - x.StartTime.ToTimeSpan()).TotalHours);

        // Calculate total available room-hours.
        double availableRoomHours =
            dashboard.TotalRooms * officeHoursPerDay;

        // Calculate utilization percentage.
        double utilization = availableRoomHours == 0
            ? 0
            : (bookedRoomHours / availableRoomHours) * 100;

        // Utilization cannot exceed 100%.
        utilization = Math.Min(utilization, 100.0);

        dashboard.Utilization = Math.Round(utilization, 2);

        // =========================================================
        // PENDING APPROVAL LIST
        // =========================================================

        dashboard.PendingApprovalList =
            await _context.Bookings
            .Include(x => x.Room)
            .Include(x => x.Employee)
            .Where(x => x.Status == "Pending")
            .OrderBy(x => x.BookingDate)
            .Take(5)
            .Select(x => new PendingApprovalDto
            {
                BookingId = x.BookingId,
                RoomName = x.Room!.RoomName,
                BookingDate = x.BookingDate,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                RequestedBy = x.Employee!.Name
            })
            .ToListAsync();

        // =========================================================
        // RECENT BOOKINGS
        // =========================================================

        dashboard.RecentBookings =
            await _context.Bookings
            .Include(x => x.Room)
            .OrderByDescending(x => x.BookedOn)
            .Take(5)
            .Select(x => new RecentBookingDto
            {
                RoomName = x.Room!.RoomName,
                BookingDate = x.BookingDate,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Status = x.Status
            })
            .ToListAsync();

        // =========================================================
        // NOTIFICATIONS
        // =========================================================
        // Until Notifications table is implemented

        dashboard.Notifications = new List<NotificationDto>();

        // =========================================================
        // RETURN DASHBOARD
        // =========================================================

        return dashboard;
    }
}