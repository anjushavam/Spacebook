namespace SpaceBook.Application.DTOs.Room;

public class RoomDto
{
    public int RoomId { get; set; }

    public string RoomName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;

    public string RoomType { get; set; } = string.Empty;

    public int Capacity { get; set; }

    // Foreign key
    public int ModuleId { get; set; }

    // Display name
    public string Module { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public bool IsBlocked { get; set; }

    public List<string> Facilities { get; set; } = new();
}