using System.Text.Json.Serialization;

namespace SpaceBook.Application.DTOs.Employee;

public class NotificationDto
{
    public int NotificationId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedOn { get; set; }

    public string TimeAgo { get; set; } = string.Empty;


    // =========================================================
    // Booking Information
    // =========================================================

    public string? EmployeeName { get; set; }

    public string? RoomName { get; set; }

    public DateOnly? BookingDate { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }


    // =========================================================
    // Internal CreatedAt
    // =========================================================

    [JsonIgnore]
    public DateTime CreatedAt { get; set; }
}