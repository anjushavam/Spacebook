namespace SpaceBook.Application.DTOs.Copilot;

public class CopilotUserProfileDto
{
    public int EmployeeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
