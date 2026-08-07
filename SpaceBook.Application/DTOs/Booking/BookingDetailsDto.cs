namespace SpaceBook.Application.DTOs.Booking;

public class BookingDetailsDto
{
    public int BookingId { get; set; }

    public int EmployeeId { get; set; }

    public string MeetingTitle { get; set; } = string.Empty;

    public string Purpose { get; set; } = string.Empty;

    public int ParticipantCount { get; set; }

    public string RoomName { get; set; } = string.Empty;

    public string EmployeeName { get; set; } = string.Empty;

    public DateOnly BookingDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime BookedOn { get; set; }
}