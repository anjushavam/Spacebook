namespace SpaceBook.Domain.Entities;
 
public class RoomType

{

    public int RoomTypeId { get; set; }
 
    public string TypeName { get; set; } = string.Empty;
 
    public ICollection<Room> Rooms { get; set; } = new List<Room>();

}
 