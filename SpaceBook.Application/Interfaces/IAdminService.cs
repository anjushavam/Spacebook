using SpaceBook.Application.DTOs.Admin;
 
public interface IAdminService
{
    Task<AdminDashboardDto> GetDashboardAsync();
}