using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
using SpaceBook.Infrastructure.Data;

namespace SpaceBook.Infrastructure.Repositories;

public class EmployeeCheckInRepository 
    : IEmployeeCheckInRepository
{
    private readonly ApplicationDbContext _context;


    public EmployeeCheckInRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }


    public async Task<Booking?> GetBookingAsync(
        int bookingId,
        int employeeId)
    {
        return await _context.Bookings
            .FirstOrDefaultAsync(b =>
                b.BookingId == bookingId &&
                b.EmployeeId == employeeId);
    }


    public async Task AddAsync(CheckIn checkIn)
    {
        await _context.CheckIns.AddAsync(checkIn);

        await _context.SaveChangesAsync();
    }
}