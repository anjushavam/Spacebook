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
        // ROLE MAPPING
        // =====================================================

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");

            entity.HasKey(r => r.RoleId);

            entity.Property(r => r.RoleId)
                .HasColumnName("roleid");

            entity.Property(r => r.RoleName)
                .HasColumnName("rolename")
                .HasMaxLength(100);
        });


        // =====================================================
        // EMPLOYEE MAPPING
        // =====================================================

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("employees");

            entity.HasKey(e => e.EmployeeId);

            entity.Property(e => e.EmployeeId)
                .HasColumnName("employeeid");

            entity.Property(e => e.RoleId)
                .HasColumnName("roleid");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(200);

            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasMaxLength(255);

            entity.Property(e => e.PasswordHash)
                .HasColumnName("passwordhash");

            entity.Property(e => e.Department)
                .HasColumnName("department")
                .HasMaxLength(200);

            entity.Property(e => e.IsActive)
                .HasColumnName("isactive");

            entity.Property(e => e.CreatedOn)
                .HasColumnName("createdon");

            // Employee -> Role
            entity.HasOne(e => e.Role)
                .WithMany()
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Employee -> Bookings
            entity.HasMany(e => e.Bookings)
                .WithOne(b => b.Employee)
                .HasForeignKey(b => b.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });


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
        // ROOM TYPE MAPPING
        // =====================================================

        modelBuilder.Entity<RoomType>(entity =>
        {
            entity.ToTable("roomtypes");

            entity.HasKey(r => r.RoomTypeId);

            entity.Property(r => r.RoomTypeId)
                .HasColumnName("roomtypeid");

            entity.Property(r => r.TypeName)
                .HasColumnName("typename")
                .HasMaxLength(100);
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

            // Room -> RoomType
            entity.HasOne(r => r.RoomType)
                .WithMany()
                .HasForeignKey(r => r.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Room -> Module
            entity.HasOne(r => r.Module)
                .WithMany(m => m.Rooms)
                .HasForeignKey(r => r.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =====================================================
        // ROOM FACILITY MAPPING
        // =====================================================

        modelBuilder.Entity<RoomFacility>(entity =>
        {
            entity.ToTable("roomfacilities");

            entity.HasKey(rf => new
            {
                rf.RoomId,
                rf.FacilityId
            });

            entity.Property(rf => rf.RoomId)
                .HasColumnName("roomid");

            entity.Property(rf => rf.FacilityId)
                .HasColumnName("facilityid");

            entity.HasOne(rf => rf.Room)
                .WithMany(r => r.RoomFacilities)
                .HasForeignKey(rf => rf.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rf => rf.Facility)
                .WithMany()
                .HasForeignKey(rf => rf.FacilityId)
                .OnDelete(DeleteBehavior.Cascade);
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