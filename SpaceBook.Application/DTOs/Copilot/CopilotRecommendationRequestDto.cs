namespace SpaceBook.Application.DTOs.Copilot;

public class CopilotRecommendationRequestDto
{
    public DateOnly Date { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public int ParticipantCount { get; set; }

    public int? OfficeId { get; set; }

    public int? RoomTypeId { get; set; }

    public string? Facility { get; set; }
}