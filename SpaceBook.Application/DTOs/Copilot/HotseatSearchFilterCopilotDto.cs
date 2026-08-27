namespace SpaceBook.Application.DTOs.Copilot;

public class HotseatSearchFilterCopilotDto
{
    public string? Search { get; set; }

    public DateOnly? Date { get; set; }

    public string? Location { get; set; }

    public int? OfficeId { get; set; }

    public string? Office { get; set; }

    public string? Module { get; set; }

    public string? Section { get; set; }

    public string? Status { get; set; }
}
