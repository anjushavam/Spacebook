using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Admin;
using SpaceBook.Application.Interfaces;
using SpaceBook.Infrastructure.Data;

namespace SpaceBook.Infrastructure.Repositories;

public class FacilityRepository : IFacilityRepository
{
    private readonly ApplicationDbContext _context;

    public FacilityRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FacilityDto>> GetFacilitiesAsync()
    {
        return await _context.Facilities
            .AsNoTracking()
            .OrderBy(x => x.FacilityName)
            .Select(x => new FacilityDto
            {
                FacilityId = x.FacilityId,
                FacilityName = x.FacilityName
            })
            .ToListAsync();
    }
}