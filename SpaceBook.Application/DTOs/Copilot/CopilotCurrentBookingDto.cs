namespace SpaceBook.Application.DTOs.Copilot;

public class CopilotCurrentBookingDto
{

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string Status { get; set; } = string.Empty;
}