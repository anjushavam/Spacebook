namespace SpaceBook.Domain.Entities;

public class Booking
{
    public int BookingId { get; set; }

    public int RoomId { get; set; }

    public int EmployeeId { get; set; }

    public string MeetingTitle { get; set; } =
        string.Empty;

    public int ParticipantCount { get; set; }

    public DateOnly BookingDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public DateTime BookedOn { get; set; }

    public string Status { get; set; } =
        string.Empty;

    public string? CancellationReason { get; set; }

    public bool StartReminderSent { get; set; } = false;

    public bool EndReminderSent { get; set; } = false;

    public Room? Room { get; set; }

    public Employee? Employee { get; set; }

    public CheckIn? CheckIn { get; set; }

    public ICollection<BookingEmailNotification> EmailNotifications { get; set; } =
        new List<BookingEmailNotification>();
}