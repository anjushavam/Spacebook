namespace SpaceBook.Domain.Entities;

public class Room
{
    public int RoomId { get; set; }

    public int RoomTypeId { get; set; }

    public int ModuleId { get; set; }

    public string RoomName { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool IsBlocked { get; set; } = false;

    // Navigation properties

    public RoomType? RoomType { get; set; }

    public Module? Module { get; set; }

    public ICollection<Booking> Bookings { get; set; }
        = new List<Booking>();

    public ICollection<RoomFacility> RoomFacilities { get; set; }
        = new List<RoomFacility>();
}