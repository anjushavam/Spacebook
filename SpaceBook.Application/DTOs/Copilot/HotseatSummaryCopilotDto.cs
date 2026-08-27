namespace SpaceBook.Application.DTOs.Copilot;

public class HotseatSummaryCopilotDto
{
    public DateOnly Date { get; set; }

    public int TotalSeats { get; set; }

    public int AvailableSeats { get; set; }

    public int BookedSeats { get; set; }

    public int CheckedInSeats { get; set; }

    public int CancelledBookings { get; set; }

    public int ExpiredBookings { get; set; }

    public int ReleasedBookings { get; set; }

    public List<HotseatLocationCopilotDto> Locations { get; set; } = new();
}
