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

    // =========================================================
    // DB SETS
    // =========================================================

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Employee> Employees => Set<Employee>();
    

    public DbSet<RoomType> RoomTypes => Set<RoomType>();

    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<Module> Modules => Set<Module>();

    public DbSet<Facility> Facilities => Set<Facility>();

    public DbSet<RoomFacility> RoomFacilities => Set<RoomFacility>();

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<CheckIn> CheckIns => Set<CheckIn>();

    public DbSet<Notification> Notifications => Set<Notification>();


    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =====================================================
        // APPLY ENTITY CONFIGURATIONS
        // =====================================================

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);


        // =====================================================
        // FACILITY MAPPING
        // =====================================================

        modelBuilder.Entity<Facility>(entity =>
        {
            entity.ToTable("facilities");

            entity.HasKey(f => f.FacilityId);

            entity.Property(f => f.FacilityId)
                .HasColumnName("facilityid");

            entity.Property(f => f.FacilityName)
                .HasColumnName("facilityname")
                .HasMaxLength(100);
        });


        // =====================================================
        // MODULE MAPPING
        // =====================================================

        modelBuilder.Entity<Module>(entity =>
        {
            entity.ToTable("modules");

            entity.HasKey(m => m.ModuleId);

            entity.Property(m => m.ModuleId)
                .HasColumnName("moduleid");

            entity.Property(m => m.OfficeId)
                .HasColumnName("officeid");

            entity.Property(m => m.ModuleName)
                .HasColumnName("modulename");

            entity.Property(m => m.RecordIngestedBy)
                .HasColumnName("recordingestedby");

            entity.Property(m => m.RecordIngestedOn)
                .HasColumnName("recordingestedon");

            entity.Property(m => m.RecordModifiedBy)
                .HasColumnName("recordmodifiedby");

            entity.Property(m => m.RecordModifiedOn)
                .HasColumnName("recordmodifiedon");
        });


        // =====================================================
        // ROOM MAPPING
        // =====================================================

        modelBuilder.Entity<Room>(entity =>
{
    entity.ToTable("rooms");

    entity.HasKey(r => r.RoomId);

    entity.Property(r => r.RoomId)
        .HasColumnName("roomid");

    entity.Property(r => r.RoomTypeId)
        .HasColumnName("roomtypeid");

    entity.Property(r => r.RoomName)
        .HasColumnName("roomname");

    entity.Property(r => r.Capacity)
        .HasColumnName("capacity");

    entity.Property(r => r.ModuleId)
        .HasColumnName("moduleid");

    entity.Property(r => r.Status)
        .HasColumnName("status");

    entity.Property(r => r.IsBlocked)
        .HasColumnName("isblocked");

    entity.HasOne(r => r.RoomType)
        .WithMany()
        .HasForeignKey(r => r.RoomTypeId)
        .OnDelete(DeleteBehavior.Restrict);

    entity.HasOne(r => r.Module)
        .WithMany(m => m.Rooms)
        .HasForeignKey(r => r.ModuleId)
        .OnDelete(DeleteBehavior.Restrict);
});

        // =====================================================
        // CHECK-IN MAPPING
        // =====================================================

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
                .HasForeignKey<CheckIn>(c => c.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        // =====================================================
        // NOTIFICATION MAPPING
        // =====================================================

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