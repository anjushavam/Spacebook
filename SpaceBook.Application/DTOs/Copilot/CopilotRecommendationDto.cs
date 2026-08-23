namespace SpaceBook.Application.DTOs.Copilot;

public class CopilotRecommendationDto
{
    public int RoomId { get; set; }

    public string RoomName { get; set; } = string.Empty;

    public string RoomType { get; set; } = string.Empty;

    public string OfficeName { get; set; } = string.Empty;

    public string LocationName { get; set; } = string.Empty;

    public string ModuleName { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public List<string> Facilities { get; set; } = new();

    public bool IsAvailable { get; set; }

    public int MatchScore { get; set; }

    public string MatchReason { get; set; } = string.Empty;
}