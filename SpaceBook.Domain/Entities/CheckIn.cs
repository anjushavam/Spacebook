namespace SpaceBook.Domain.Entities;

public class CheckIn
{
    public int CheckInId { get; set; }

    public int BookingId { get; set; }

    public DateTime CheckedInAt { get; set; }

    public string Status { get; set; } = string.Empty;


    public Booking? Booking { get; set; }
}