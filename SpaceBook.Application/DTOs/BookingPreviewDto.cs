namespace SpaceBook.Application.DTOs.Employee;
 
public class BookingPreviewDto
{
    
 
    public TimeOnly StartTime { get; set; }
 
    public TimeOnly EndTime { get; set; }
 
    public string Status { get; set; } = string. Empty;
}