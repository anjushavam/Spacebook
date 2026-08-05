namespace SpaceBook.Application.DTOs.Employee;
 
public class TimeSlotDto
{
    public TimeOnly StartTime { get; set; }
 
    public TimeOnly EndTime { get; set; }
 
    public bool IsBooked { get; set; }
}