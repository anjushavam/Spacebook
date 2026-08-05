namespace SpaceBook.Application.DTOs.Booking;

public class BookingDto
{
    public int BookingId { get; set; }

    public string Purpose { get; set; } = string.Empty;

    public string RoomName { get; set; } = string.Empty;

    public string EmployeeName { get; set; } = string.Empty;

    public DateOnly BookingDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string Status { get; set; } = string.Empty;
}