namespace SpaceBook.Application.DTOs.Employee;
 
public class AvailabilityCalendarDto
{
    public DateOnly Date { get; set; }
 
    public List<RoomAvailabilityDto> Rooms { get; set; } = new();
}