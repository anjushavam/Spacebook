namespace SpaceBook.Application.DTOs.Hotseat;

public class HotseatFilterOptionsDto
{
    public List<FilterOptionItemDto> Timeframes { get; set; } = new();

    public List<FilterOptionItemDto> Modules { get; set; } = new();

    public List<FilterOptionItemDto> Statuses { get; set; } = new();

    public List<string> Sections { get; set; } = new();
}

public class FilterOptionItemDto
{
    public string Value { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string? Group { get; set; }
}
