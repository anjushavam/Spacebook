namespace SpaceBook.Application.DTOs.Hotseat;

public class HotseatDto
{
    public int HotseatBookingId { get; set; }

    public int SeatId { get; set; }

    public string SeatNumber { get; set; } = string.Empty;

    public string? Section { get; set; }

    public string BookingDate { get; set; } = string.Empty;

    public string BookingStatus { get; set; } = string.Empty;

    public int EmployeeId { get; set; }

    public DateTime? CheckInTime { get; set; }

    public DateTime? ReleasedOn { get; set; }

    public string City { get; set; } = string.Empty;

    public string Building { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;
}