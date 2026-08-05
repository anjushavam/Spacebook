namespace SpaceBook.Application.DTOs.Employee;

public class EmployeeDashboardDto
{
    public int BookingsToday { get; set; }

    public int UpcomingCount { get; set; }

    public List<RecentReservationDto> RecentReservations { get; set; } = new();

    public List<TodayMeetingDto> TodayMeetings { get; set; } = new();
}