namespace SpaceBook.Application.DTOs.Booking;

public class RejectBookingDto
{
    public string Status { get; set; } = "Rejected";

    public string? Reason { get; set; }
}