namespace SpaceBook.Domain.Entities;

public class Location
{
    public int LocationId { get; set; }

    public string LocationName { get; set; } = string.Empty;

    public ICollection<Office> Offices { get; set; }
        = new List<Office>();
}