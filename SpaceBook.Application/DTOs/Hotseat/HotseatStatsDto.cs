namespace SpaceBook.Application.DTOs.Hotseat;

public class HotseatStatsDto
{
    public int TotalSpaces { get; set; }

    public int Available { get; set; }

    public int Booked { get; set; }

    public int PendingCheckIn { get; set; }

    public int BookingsToday { get; set; }
}