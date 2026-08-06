using Microsoft.EntityFrameworkCore;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
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

    public DbSet<CheckIn> CheckIns => Set<CheckIn>();

    public DbSet<Notification> Notifications => Set<Notification>();



    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);



        // Room Mapping
        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("rooms");

            entity.Property(r => r.RoomId)
                .HasColumnName("roomid");

            entity.Property(r => r.RoomName)
                .HasColumnName("roomname");

            entity.Property(r => r.Capacity)
                .HasColumnName("capacity");

            entity.Property(r => r.Module)
                .HasColumnName("module");

            entity.Property(r => r.Status)
                .HasColumnName("status");

            entity.Property(r => r.IsBlocked)
                .HasColumnName("isblocked");
        });



        // CheckIn Mapping
        modelBuilder.Entity<CheckIn>(entity =>
        {
            entity.ToTable("checkins");

            entity.HasKey(c => c.CheckInId);

            entity.Property(c => c.CheckInId)
                .HasColumnName("checkinid")
                .ValueGeneratedOnAdd();

            entity.Property(c => c.BookingId)
                .HasColumnName("bookingid");

            entity.Property(c => c.CheckedInAt)
                .HasColumnName("checkedinat");

            entity.Property(c => c.Status)
                .HasColumnName("status")
                .HasMaxLength(50);


            entity.HasOne(c => c.Booking)
                .WithOne(b => b.CheckIn)
                .HasForeignKey<CheckIn>(
                    c => c.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });



        // Notification Mapping
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");


            entity.HasKey(n => n.NotificationId);


            entity.Property(n => n.NotificationId)
                .HasColumnName("notificationid");


            entity.Property(n => n.EmployeeId)
                .HasColumnName("employeeid");


            entity.Property(n => n.BookingId)
                .HasColumnName("bookingid");


            entity.Property(n => n.Message)
                .HasColumnName("message")
                .HasMaxLength(500);


            entity.Property(n => n.IsRead)
                .HasColumnName("isread");


            entity.Property(n => n.CreatedAt)
                .HasColumnName("createdat");


            entity.HasOne(n => n.Booking)
                .WithMany()
                .HasForeignKey(n => n.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}