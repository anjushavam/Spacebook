namespace SpaceBook.Application.DTOs.Employee;
 
public class RoomAvailabilityDto

{

    public int RoomId { get; set; }
 
    public string RoomName { get; set; } = string.Empty;
 
    public string RoomType { get; set; } = string.Empty;
 
    public string Module { get; set; } = string.Empty;
 
    public int Capacity { get; set; }
 
    public string Status { get; set; } = string.Empty;
 
    public int AvailableSlots { get; set; }
 
    public List<TimeSlotDto> TimeSlots { get; set; } = new();
 
    public BookingPreviewDto? CurrentBooking { get; set; }

}
 