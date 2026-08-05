namespace SpaceBook.Application.DTOs.Employee;
 
public class EmployeeDashboardDto
{
    public int TotalRooms { get; set; }
 
    public int AvailableNow { get; set; }
 
    public int BookingsToday { get; set; }
 
    public int UpcomingBookingsCount { get; set; }
 
    public List<EmployeeUpcomingBookingDto> UpcomingBookings { get; set; }
        = new();
 
    public List<TodayMeetingDto> TodayMeetings { get; set; }
        = new();
}