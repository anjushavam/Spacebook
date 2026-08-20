using System.ComponentModel.DataAnnotations;
 
namespace SpaceBook.Application.DTOs.Room;
 
public class CreateRoomDto
{
    [Required(ErrorMessage = "Room name is required.")]
    public string RoomName { get; set; } = string.Empty;
 
    [Range(1, int.MaxValue, ErrorMessage = "Room type is required.")]
    public int RoomTypeId { get; set; }
 
    [Range(1, int.MaxValue, ErrorMessage = "Room capacity must be greater than zero.")]
    public int Capacity { get; set; }
 
    [Range(1, int.MaxValue, ErrorMessage = "Module is required.")]
    public int ModuleId { get; set; }
 
    [Required(ErrorMessage = "Room status is required.")]
    public string Status { get; set; } = "Available";
 
    public List<int> FacilityIds { get; set; } = new();
}