namespace SpaceBook.Application.DTOs.Admin;
 
public class RecentBookingDto
{
    public string RoomName { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
}