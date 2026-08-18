namespace SpaceBook.Domain.Entities;

public class Seat
{
    public int SeatId { get; set; }

    public int ModuleId { get; set; }

    public string? Section { get; set; }

    public string SeatNumber { get; set; } = string.Empty;

    public string RowNumber { get; set; } = string.Empty;

    public int ColumnNumber { get; set; }

    public bool IsActive { get; set; }

    public Module? Module { get; set; }

    public ICollection<HotseatBooking> HotseatBookings { get; set; }
        = new List<HotseatBooking>();
}