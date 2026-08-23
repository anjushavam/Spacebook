namespace SpaceBook.Domain.Entities;

public class HotseatBooking
{
    public int HotseatBookingId { get; set; }

    public int SeatId { get; set; }

    public int EmployeeId { get; set; }

    public DateOnly BookingDate { get; set; }

    public string BookingStatus { get; set; } = "Confirmed";

    public DateTime BookedOn { get; set; }

    public DateTime? CheckInDeadline { get; set; }

    public DateTime? CheckInTime { get; set; }

    public DateTime? ReleasedOn { get; set; }

    public string? RecordIngestedBy { get; set; }

    public DateTime? RecordIngestedOn { get; set; }

    public string? RecordModifiedBy { get; set; }

    public DateTime? RecordModifiedOn { get; set; }

    // Navigation properties
    public Seat? Seat { get; set; }

    public Employee? Employee { get; set; }
}