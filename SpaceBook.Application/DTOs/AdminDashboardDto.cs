namespace SpaceBook.Application.DTOs.Admin;
 
public class AdminDashboardDto
{
    public int TotalRooms { get; set; }
    public int TodayBookings { get; set; }
    public int PendingApprovals { get; set; }
    public double Utilization { get; set; }
 
    public List<PendingApprovalDto> PendingApprovalList { get; set; } = [];
    public List<RecentBookingDto> RecentBookings { get; set; } = [];
    public List<NotificationDto> Notifications { get; set; } = [];
}