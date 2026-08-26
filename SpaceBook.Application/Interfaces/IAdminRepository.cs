using SpaceBook.Application.DTOs.Admin;
 
public interface IAdminRepository
{
    Task<AdminDashboardDto> GetDashboardAsync(AdminDashboardFilterDto? filter = null);
}