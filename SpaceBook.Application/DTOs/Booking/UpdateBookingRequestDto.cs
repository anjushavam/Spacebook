namespace SpaceBook.Application.DTOs.Booking;

public class UpdateBookingRequestDto
{
    public string MeetingTitle { get; set; } = string.Empty;

    public string Purpose { get; set; } = string.Empty;

    public int RoomId { get; set; }

    public DateOnly BookingDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public int ParticipantCount { get; set; }
}