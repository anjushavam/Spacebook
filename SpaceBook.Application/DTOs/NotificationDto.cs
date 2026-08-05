namespace SpaceBook.Application.DTOs.Admin;
 
public class NotificationDto
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
}