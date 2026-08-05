namespace SpaceBook.Application.DTOs.Room;

public class UpdateRoomDto
{
    public int RoomTypeId { get; set; }

    public string RoomName { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public string Module { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public List<int> FacilityIds { get; set; } = new();
}