namespace SpaceBook.Application.DTOs.Copilot;

public class CopilotAvailabilityResponseDto
{
    public DateOnly Date { get; set; }

    public List<CopilotRoomAvailabilityDto> Rooms { get; set; }
        = new();
}