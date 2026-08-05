namespace SpaceBook.Application.DTOs.Room;

public class RoomDetailsDto
{
    public int RoomId { get; set; }

    public int RoomTypeId { get; set; }

    public string RoomName { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public string Module { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public List<string> Facilities { get; set; } = new();
}