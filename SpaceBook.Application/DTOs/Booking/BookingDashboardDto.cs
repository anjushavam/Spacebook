namespace SpaceBook.Application.DTOs.Booking;

public class BookingDashboardDto
{
    public int PendingRequests { get; set; }

    public int Confirmed { get; set; }

    public int Cancelled { get; set; }
}