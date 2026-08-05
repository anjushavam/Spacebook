using SpaceBook.Application.DTOs.Admin;
using SpaceBook.Application.Interfaces;
 
namespace SpaceBook.Application.Services;
 
public class AdminService : IAdminService
{
    private readonly IAdminRepository _repository;
 
    public AdminService(IAdminRepository repository)
    {
        _repository = repository;
    }
 
    public async Task<AdminDashboardDto> GetDashboardAsync()
    {
        return await _repository.GetDashboardAsync();
    }
}