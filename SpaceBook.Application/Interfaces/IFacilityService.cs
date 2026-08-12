using SpaceBook.Application.DTOs.Admin;

namespace SpaceBook.Application.Interfaces;

public interface IFacilityService
{
    Task<List<FacilityDto>> GetFacilitiesAsync();
}