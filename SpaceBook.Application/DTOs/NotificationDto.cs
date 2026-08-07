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

    [JsonIgnore] // Hides the duplicate empty default date field from JSON responses
    public DateTime CreatedAt { get; set; }
}