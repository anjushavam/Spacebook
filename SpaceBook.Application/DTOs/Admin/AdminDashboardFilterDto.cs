namespace SpaceBook.Application.DTOs.Admin;

public class AdminDashboardFilterDto
{
    public string? Timeframe { get; set; }
    public string? Module { get; set; }
    public string? Status { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int? RoomTypeId { get; set; }
}
