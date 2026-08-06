namespace SpaceBook.Domain.Entities;
 
public class Room
{
    public int RoomId { get; set; }
 
    public int RoomTypeId { get; set; }
 
    public string RoomName { get; set; } = string.Empty;
 
    public int Capacity { get; set; }
 
    public string Module { get; set; } = string.Empty;
 
    public string Status { get; set; } = string.Empty;
 
    public RoomType? RoomType { get; set; }
 
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public bool IsBlocked { get; set; } = false;
 
    public ICollection<RoomFacility> RoomFacilities { get; set; } = new List<RoomFacility>();
}