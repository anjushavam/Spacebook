namespace SpaceBook.Application.DTOs.Hotseat;

public class HotseatDto
{
    public int SeatId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Row { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}