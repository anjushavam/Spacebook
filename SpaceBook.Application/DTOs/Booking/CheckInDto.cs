namespace SpaceBook.Application.DTOs.Booking;

public class CheckInDto
{
    public int BookingId { get; set; }

    public DateTime CheckedInAt { get; set; }

    public string Status { get; set; } = string.Empty;
}