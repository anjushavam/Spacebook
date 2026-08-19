namespace SpaceBook.Application.DTOs.Copilot;
 
public class RoomCopilotDto
{
    public int RoomId { get; set; }
 
    public string RoomName { get; set; } = string.Empty;
 
    public string OfficeName { get; set; } = string.Empty;
 
    public string LocationName { get; set; } = string.Empty;
 
    public string ModuleName { get; set; } = string.Empty;
 
    public int Capacity { get; set; }
 
    public string Status { get; set; } = string.Empty;
 
    public bool IsBlocked { get; set; }
 
    public List<string> Facilities { get; set; } = new();
}