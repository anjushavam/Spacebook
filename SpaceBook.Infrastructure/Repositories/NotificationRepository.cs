using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
using SpaceBook.Infrastructure.Data;
 
namespace SpaceBook.Infrastructure.Repositories;
 
public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;
 
 
    public NotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }
 
 
    public async Task AddAsync(Notification notification)
    {
        await _context.Notifications.AddAsync(notification);
 
        await _context.SaveChangesAsync();
    }
 
 
    // Admin - all notifications
    public async Task<List<Notification>> GetAllAsync()
    {
        return await _context.Notifications
            .Include(x => x.Employee)
            .Include(x => x.Booking)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }
 
 
    // Employee - own notifications only
    public async Task<List<Notification>> GetEmployeeNotificationsAsync(
        int employeeId)
    {
        return await _context.Notifications
            .Where(x => x.EmployeeId == employeeId)
            .Include(x => x.Booking)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }
}