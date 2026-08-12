using SpaceBook.Application.DTOs.Admin;
using SpaceBook.Application.Interfaces;

namespace SpaceBook.Application.Services;

public class FacilityService : IFacilityService
{
    private readonly IFacilityRepository _repository;

    public FacilityService(IFacilityRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<FacilityDto>> GetFacilitiesAsync()
    {
        return await _repository.GetFacilitiesAsync();
    }
}