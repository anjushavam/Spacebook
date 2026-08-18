namespace SpaceBook.Application.DTOs.Hotseat;

public class HotseatAvailabilityDto
{
    public int SeatNumber { get; set; }

    public string Section { get; set; } = string.Empty;

    public string Row { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}