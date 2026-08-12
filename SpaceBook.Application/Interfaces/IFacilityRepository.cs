using SpaceBook.Application.DTOs.Admin;

namespace SpaceBook.Application.Interfaces;

public interface IFacilityRepository
{
    Task<List<FacilityDto>> GetFacilitiesAsync();
}