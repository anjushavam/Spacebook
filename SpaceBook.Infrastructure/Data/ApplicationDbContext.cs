using Microsoft.EntityFrameworkCore;
using SpaceBook.Domain.Entities;
 
namespace SpaceBook.Infrastructure.Data;
 
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
 
    public DbSet<Role> Roles => Set<Role>();
 
    public DbSet<Employee> Employees => Set<Employee>();
 
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
 
    public DbSet<Room> Rooms => Set<Room>();
 
    public DbSet<Facility> Facilities => Set<Facility>();
 
    public DbSet<RoomFacility> RoomFacilities => Set<RoomFacility>();
 
    public DbSet<Booking> Bookings => Set<Booking>();
 
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
 
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}