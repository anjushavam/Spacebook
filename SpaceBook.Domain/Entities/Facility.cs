namespace SpaceBook.Domain.Entities;
 
public class Facility
{
    public int FacilityId { get; set; }
 
    public string FacilityName { get; set; } = string.Empty;
 
    public ICollection<RoomFacility> RoomFacilities { get; set; } = new List<RoomFacility>();
}