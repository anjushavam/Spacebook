namespace SpaceBook.Domain.Entities;

public class Notification
{
    public int NotificationId { get; set; }

    public int? EmployeeId { get; set; }

    public int? BookingId { get; set; }

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public Employee? Employee { get; set; }

    public Booking? Booking { get; set; }
}