namespace SpaceBook.Application.DTOs.Room;

public class CreateRoomDto
{
    public int RoomTypeId { get; set; }

    public string RoomName { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public int ModuleId { get; set; }

    public string Status { get; set; } = "Available";

    public List<int> FacilityIds { get; set; } = new();
}