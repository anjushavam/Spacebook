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
        var today = DateOnly.FromDateTime(DateTime.Today);
 
        var dashboard = new AdminDashboardDto();
 
        dashboard.TotalRooms = await _context.Rooms.CountAsync();
 
        dashboard.TodayBookings = await _context.Bookings
            .CountAsync(x => x.BookingDate == today);
 
        dashboard.PendingApprovals = await _context.Bookings
            .CountAsync(x => x.Status == "Pending");
 
        // Fixed: Use today's bookings count instead of total lifetime bookings, 
        // and cap utilization at 100% max.
        double utilization = dashboard.TotalRooms == 0 
            ? 0 
            : ((double)dashboard.TodayBookings / dashboard.TotalRooms) * 100;

        utilization = Math.Min(utilization, 100.0);
        dashboard.Utilization = Math.Round(utilization, 2);
 
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
 
        // Until Notifications table is implemented
        dashboard.Notifications = new List<NotificationDto>();
 
        return dashboard;
    }
}