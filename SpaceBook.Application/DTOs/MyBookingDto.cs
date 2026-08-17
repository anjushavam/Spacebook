namespace SpaceBook.Application.DTOs.Employee;
 
public class MyBookingDto

{

    public int BookingId { get; set; }

    public int RoomId { get; set; }
 
    public string RoomName { get; set; } = string.Empty;
    
 public string Module { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
 
    public DateOnly BookingDate { get; set; }
 
    public TimeOnly StartTime { get; set; }
 
    public TimeOnly EndTime { get; set; }
 
    public string Status { get; set; } = string.Empty;

}
 