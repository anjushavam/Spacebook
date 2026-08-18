namespace SpaceBook.Application.DTOs.Room;
 
public class BulkCreateRoomDto
{
    public int RoomTypeId { get; set; }
 
    public int Count { get; set; }
 
    public int Capacity { get; set; }
 
    public int ModuleId { get; set; }
 
    public string Status { get; set; } = "Available";
 
    public List<int> FacilityIds { get; set; } = new();
}