namespace SpaceBook.Application.DTOs.Copilot;

public class CopilotRoomAvailabilityDto
{
    public int RoomId { get; set; }

    public string RoomName { get; set; } = string.Empty;

    public string RoomType { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public List<string> Facilities { get; set; } = new();

    public string Status { get; set; } = string.Empty;

    public int AvailableSlots { get; set; }

    public List<CopilotTimeSlotDto> TimeSlots { get; set; }
        = new();

    public CopilotCurrentBookingDto? CurrentBooking { get; set; }
}