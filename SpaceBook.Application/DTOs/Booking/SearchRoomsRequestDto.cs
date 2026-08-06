public class SearchRoomsRequestDto
{
    public string Module { get; set; } = string.Empty;

    public int RoomTypeId { get; set; }

    public int ParticipantCount { get; set; }

    public List<int> FacilityIds { get; set; } = new();

    public DateOnly BookingDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }
}