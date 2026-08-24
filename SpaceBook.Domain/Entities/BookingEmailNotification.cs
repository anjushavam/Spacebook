namespace SpaceBook.Domain.Entities;

public class BookingEmailNotification
{
    public int BookingEmailNotificationId { get; set; }

    public int BookingId { get; set; }

    public string NotificationType { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = "Sent";

    public Booking? Booking { get; set; }
}
