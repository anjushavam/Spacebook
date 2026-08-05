using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
using SpaceBook.Infrastructure.Data;

namespace SpaceBook.Infrastructure.Repositories;
public class EmployeeRepository : IEmployeeRepository{
    private readonly ApplicationDbContext _context;

    public EmployeeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Employee?> GetByEmailAsync(string email)
    {
        return await _context.Employees            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email == email);
    }
}