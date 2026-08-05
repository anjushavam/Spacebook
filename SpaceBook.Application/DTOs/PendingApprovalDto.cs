namespace SpaceBook.Application.DTOs.Admin;
 
public class PendingApprovalDto
{
    public int BookingId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string RequestedBy { get; set; } = string. Empty;
}