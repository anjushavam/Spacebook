using SpaceBook.Application.DTOs.Room;
 
namespace SpaceBook.Application.Interfaces;
 
public interface IRoomService

{

    Task<RoomDashboardDto> GetDashboardAsync();
 
    Task<IEnumerable<RoomDto>> GetAllAsync(RoomFilterDto filter);
 
    Task<RoomDetailsDto?> GetByIdAsync(int roomId);
 
    Task CreateAsync(CreateRoomDto dto);
 
    Task UpdateAsync(int roomId, UpdateRoomDto dto);
 
    Task DeleteAsync(int roomId);
 
    Task<bool> UpdateRoomStatusAsync(

        int roomId,

        bool isBlocked);
 
    // Add this

    Task BulkCreateAsync(BulkCreateRoomDto dto);

}
 