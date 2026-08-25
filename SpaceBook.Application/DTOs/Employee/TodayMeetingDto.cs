namespace SpaceBook.Application.DTOs.Employee;

public class TodayMeetingDto
{
    public int BookingId { get; set; }
    public string? MeetingTitle { get; set; }

    public string RoomName { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string Status { get; set; } = string.Empty;
}