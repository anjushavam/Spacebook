namespace SpaceBook.Domain.Entities;

public class Module
{
    public int ModuleId { get; set; }

    public int OfficeId { get; set; }

    public string ModuleName { get; set; } = string.Empty;

    public string? RecordIngestedBy { get; set; }

    public DateTime? RecordIngestedOn { get; set; }

    public string? RecordModifiedBy { get; set; }

    public DateTime? RecordModifiedOn { get; set; }

    // Navigation property
    public ICollection<Room> Rooms { get; set; }
        = new List<Room>();
}