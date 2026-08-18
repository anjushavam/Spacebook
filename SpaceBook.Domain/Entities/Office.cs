namespace SpaceBook.Domain.Entities;

public class Office
{
    public int OfficeId { get; set; }

    public int LocationId { get; set; }

    public string OfficeName { get; set; } = string.Empty;

    public Location? Location { get; set; }

    public ICollection<Module> Modules { get; set; }
        = new List<Module>();
}