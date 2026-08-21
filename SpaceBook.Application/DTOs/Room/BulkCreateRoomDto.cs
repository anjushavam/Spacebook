using System.ComponentModel.DataAnnotations;
 
namespace SpaceBook.Application.DTOs.Room;
 
public class BulkCreateRoomDto
{
    // =========================================================
    // ROOM TYPE
    // =========================================================
 
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Room type is required.")]
    public int RoomTypeId { get; set; }
 
 
    // =========================================================
    // COUNT
    // =========================================================
 
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Room count must be greater than zero.")]
    public int Count { get; set; }
 
 
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