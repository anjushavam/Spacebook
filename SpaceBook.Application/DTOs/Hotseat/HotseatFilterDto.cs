namespace SpaceBook.Application.DTOs.Hotseat;

public class HotseatFilterDto
{
    public string? Timeframe { get; set; } = "All Time";

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Module { get; set; } = "All Modules";

    public string? Status { get; set; } = "All Status";

    public string? Section { get; set; }

    public string? TrendPeriod { get; set; } = "Daily";

    public string? SearchTerm { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
