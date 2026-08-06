namespace SpaceBook.Application.DTOs.Room;

public class BulkCreateRoomDto
{
    public int RoomTypeId { get; set; }

    public int Count { get; set; }

    public int Capacity { get; set; }

    public string Module { get; set; } = string.Empty;

    public string Status { get; set; } = "Available";

    public List<int> FacilityIds { get; set; } = new();
}