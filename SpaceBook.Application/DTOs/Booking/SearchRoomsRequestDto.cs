namespace SpaceBook.Application.DTOs.Booking;

public class SearchRoomsRequestDto
{
    public string? Module { get; set; }

    public int? RoomTypeId { get; set; }

    public int? ParticipantCount { get; set; }

    public List<int>? FacilityIds { get; set; }

    public DateOnly? BookingDate { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }
}