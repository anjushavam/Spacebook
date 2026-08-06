public class AvailableRoomDto
{
    public int RoomId { get; set; }
 
    public string RoomName { get; set; } = string.Empty;
 
    public string Module { get; set; } = string.Empty;
 
    public string RoomType { get; set; } = string.Empty;
 
    public int Capacity { get; set; }
 
    public List<string> Facilities { get; set; } = new();
}