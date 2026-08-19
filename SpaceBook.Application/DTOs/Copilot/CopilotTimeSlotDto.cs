namespace SpaceBook.Application.DTOs.Copilot;

public class CopilotTimeSlotDto
{
    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public bool IsBooked { get; set; }
}