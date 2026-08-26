using SpaceBook.Application.DTOs.Employee;
namespace SpaceBook.Application.DTOs.Admin;
 
public class AdminDashboardDto
{
    public int TotalRooms { get; set; }
    public int ActiveRoomsCount { get; set; }
    public int ActiveRooms { get; set; }
    public int TodayBookings { get; set; }
    public int TotalReservations { get; set; }
    public int TotalBookings { get; set; }
    public int PendingApprovals { get; set; }
    public int ConfirmedBookings { get; set; }
    public double ConfirmedRate { get; set; }
    public int CancelledBookings { get; set; }
    public double CancelledRate { get; set; }
    public double Utilization { get; set; }
    public double OccupancyRate { get; set; }
    public double UtilizationRate { get; set; }
    public double UtilizationPercentage { get; set; }
    public double Occupancy { get; set; }
    public double TotalVolumePercentage { get; set; } = 100.0;
 
    public List<PendingApprovalDto> PendingApprovalList { get; set; } = [];
    public List<RecentBookingDto> RecentBookings { get; set; } = [];
    public List<NotificationDto> Notifications { get; set; } = [];
}