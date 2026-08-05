namespace SpaceBook.Domain.Entities;
 
public class RoomFacility
{
    public int RoomFacilityId { get; set; }
 
    public int RoomId { get; set; }
 
    public int FacilityId { get; set; }
 
    public Room? Room { get; set; }
 
    public Facility? Facility { get; set; }
}