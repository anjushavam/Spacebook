using System.ComponentModel.DataAnnotations;
 
namespace SpaceBook.Application.DTOs.Room;
 
public class CreateRoomDto
{
    // =========================================================
    // ROOM NAME
    // =========================================================
 
    [Required(ErrorMessage = "Room name is required.")]
    public string RoomName { get; set; } = string.Empty;
 
 
    // =========================================================
    // ROOM TYPE
    // =========================================================
 
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Room type is required.")]
    public int RoomTypeId { get; set; }
 
 
    // =========================================================
    // CAPACITY
    // =========================================================
 
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Room capacity must be greater than zero.")]
    public int Capacity { get; set; }
 
 
    // =========================================================
    // MODULE
    // =========================================================
 
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Module is required.")]
    public int ModuleId { get; set; }
 
 
    // =========================================================
    // STATUS
    // =========================================================
 
    [Required(ErrorMessage = "Room status is required.")]
    public string Status { get; set; } = "Available";
 
 
    // =========================================================
    // FACILITIES
    // =========================================================
 
    public List<int> FacilityIds { get; set; } = new();
    public string RoomNumber { get; set; } = string.Empty;
}