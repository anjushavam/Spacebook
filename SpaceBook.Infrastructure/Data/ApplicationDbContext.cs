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
    public DbSet<HotseatBooking> HotseatBookings => Set<HotseatBooking>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Office> Offices => Set<Office>();
    public DbSet<Location> Locations => Set<Location>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        // =========================================================
        // ROLE
        // =========================================================

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");

            entity.HasKey(r => r.RoleId);

            entity.Property(r => r.RoleId)
                .HasColumnName("roleid");

            entity.Property(r => r.RoleName)
                .HasColumnName("rolename")
                .HasMaxLength(50)
                .IsRequired();

            entity.HasMany(r => r.Employees)
                .WithOne(e => e.Role)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // EMPLOYEE
        // =========================================================

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("employees");

            entity.HasKey(e => e.EmployeeId);

            entity.Property(e => e.EmployeeId)
                .HasColumnName("employeeid");

            entity.Property(e => e.RoleId)
                .HasColumnName("roleid");

            entity.Property(e => e.Name)
                .HasColumnName("name");

            entity.Property(e => e.Email)
                .HasColumnName("email");

            entity.Property(e => e.PasswordHash)
                .HasColumnName("passwordhash");

            entity.Property(e => e.Department)
                .HasColumnName("department");

            entity.Property(e => e.IsActive)
                .HasColumnName("isactive");

            entity.Property(e => e.CreatedOn)
                .HasColumnName("createdon");

            entity.HasMany(e => e.Bookings)
                .WithOne(b => b.Employee)
                .HasForeignKey(b => b.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // ROOM TYPE
        // =========================================================

        modelBuilder.Entity<RoomType>(entity =>
        {
            entity.ToTable("roomtypes");

            entity.HasKey(rt => rt.RoomTypeId);

            entity.Property(rt => rt.RoomTypeId)
                .HasColumnName("roomtypeid");

            entity.Property(rt => rt.TypeName)
                .HasColumnName("typename")
                .HasMaxLength(100)
                .IsRequired();

            entity.HasMany(rt => rt.Rooms)
                .WithOne(r => r.RoomType)
                .HasForeignKey(r => r.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // LOCATION
        // =========================================================

        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("locations");

            entity.HasKey(x => x.LocationId);

            entity.Property(x => x.LocationId)
                .HasColumnName("locationid");

            entity.Property(x => x.LocationName)
                .HasColumnName("locationname");

            entity.HasMany(x => x.Offices)
                .WithOne(x => x.Location)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // OFFICE
        // =========================================================

        modelBuilder.Entity<Office>(entity =>
        {
            entity.ToTable("offices");

            entity.HasKey(x => x.OfficeId);

            entity.Property(x => x.OfficeId)
                .HasColumnName("officeid");

            entity.Property(x => x.LocationId)
                .HasColumnName("locationid");

            entity.Property(x => x.OfficeName)
                .HasColumnName("officename");

            entity.HasOne(x => x.Location)
                .WithMany(x => x.Offices)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Modules)
                .WithOne(x => x.Office)
                .HasForeignKey(x => x.OfficeId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // MODULE
        // =========================================================

        modelBuilder.Entity<Module>(entity =>
        {
            entity.ToTable("modules");

            entity.HasKey(x => x.ModuleId);

            entity.Property(x => x.ModuleId)
                .HasColumnName("moduleid");

            entity.Property(x => x.OfficeId)
                .HasColumnName("officeid");

            entity.Property(x => x.ModuleName)
                .HasColumnName("modulename")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.RecordIngestedBy)
                .HasColumnName("recordingestedby");

            entity.Property(x => x.RecordIngestedOn)
                .HasColumnName("recordingestedon");

            entity.Property(x => x.RecordModifiedBy)
                .HasColumnName("recordmodifiedby");

            entity.Property(x => x.RecordModifiedOn)
                .HasColumnName("recordmodifiedon");

            entity.HasOne(x => x.Office)
                .WithMany(x => x.Modules)
                .HasForeignKey(x => x.OfficeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Rooms)
                .WithOne(x => x.Module)
                .HasForeignKey(x => x.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Seats)
                .WithOne(x => x.Module)
                .HasForeignKey(x => x.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // SEAT
        // =========================================================

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.ToTable("seats");

            entity.HasKey(x => x.SeatId);

            entity.Property(x => x.SeatId)
                .HasColumnName("seatid");

            entity.Property(x => x.ModuleId)
                .HasColumnName("moduleid");

            entity.Property(x => x.Section)
                .HasColumnName("section");

            entity.Property(x => x.SeatNumber)
                .HasColumnName("seatnumber");

            entity.Property(x => x.RowNumber)
                .HasColumnName("rownumber");

            entity.Property(x => x.ColumnNumber)
                .HasColumnName("columnnumber");

            entity.Property(x => x.IsActive)
                .HasColumnName("isactive");

            entity.HasOne(x => x.Module)
                .WithMany(x => x.Seats)
                .HasForeignKey(x => x.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.HotseatBookings)
                .WithOne(x => x.Seat)
                .HasForeignKey(x => x.SeatId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // ROOM
        // =========================================================

        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("rooms");

            entity.HasKey(r => r.RoomId);

            entity.Property(r => r.RoomId)
                .HasColumnName("roomid");

            entity.Property(r => r.RoomTypeId)
                .HasColumnName("roomtypeid");

            entity.Property(r => r.ModuleId)
                .HasColumnName("moduleid");

            entity.Property(r => r.RoomName)
                .HasColumnName("roomname");

            entity.Property(r => r.Capacity)
                .HasColumnName("capacity");

            entity.Property(r => r.Status)
                .HasColumnName("status");

            entity.Property(r => r.IsBlocked)
                .HasColumnName("isblocked");

            entity.HasOne(r => r.RoomType)
                .WithMany(rt => rt.Rooms)
                .HasForeignKey(r => r.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Module)
                .WithMany(m => m.Rooms)
                .HasForeignKey(r => r.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(r => r.RoomFacilities)
                .WithOne(rf => rf.Room)
                .HasForeignKey(rf => rf.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(r => r.Bookings)
                .WithOne(b => b.Room)
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // FACILITY
        // =========================================================

        modelBuilder.Entity<Facility>(entity =>
        {
            entity.ToTable("facilities");

            entity.HasKey(f => f.FacilityId);

            entity.Property(f => f.FacilityId)
                .HasColumnName("facilityid");

            entity.Property(f => f.FacilityName)
                .HasColumnName("facilityname")
                .HasMaxLength(100)
                .IsRequired();

            entity.HasMany(f => f.RoomFacilities)
                .WithOne(rf => rf.Facility)
                .HasForeignKey(rf => rf.FacilityId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        // =========================================================
        // ROOM FACILITY
        // =========================================================

        modelBuilder.Entity<RoomFacility>(entity =>
        {
            entity.ToTable("roomfacilities");

            entity.HasKey(rf => rf.RoomFacilityId);

            entity.Property(rf => rf.RoomFacilityId)
                .HasColumnName("roomfacilityid");

            entity.Property(rf => rf.RoomId)
                .HasColumnName("roomid");

            entity.Property(rf => rf.FacilityId)
                .HasColumnName("facilityid");

            entity.HasOne(rf => rf.Room)
                .WithMany(r => r.RoomFacilities)
                .HasForeignKey(rf => rf.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rf => rf.Facility)
                .WithMany(f => f.RoomFacilities)
                .HasForeignKey(rf => rf.FacilityId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        // =========================================================
        // BOOKING
        // =========================================================

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("bookings");

            entity.HasKey(b => b.BookingId);

            entity.Property(b => b.BookingId)
                .HasColumnName("bookingid");

            entity.Property(b => b.RoomId)
                .HasColumnName("roomid");

            entity.Property(b => b.EmployeeId)
                .HasColumnName("employeeid");

            entity.Property(b => b.MeetingTitle)
                .HasColumnName("meetingtitle");

            entity.Property(b => b.Purpose)
                .HasColumnName("purpose");

            entity.Property(b => b.ParticipantCount)
                .HasColumnName("participantcount");

            entity.Property(b => b.BookingDate)
                .HasColumnName("bookingdate");

            entity.Property(b => b.StartTime)
                .HasColumnName("starttime");

            entity.Property(b => b.EndTime)
                .HasColumnName("endtime");

            entity.Property(b => b.BookedOn)
                .HasColumnName("bookedon");

            entity.Property(b => b.Status)
                .HasColumnName("status");

            entity.HasOne(b => b.Employee)
                .WithMany(e => e.Bookings)
                .HasForeignKey(b => b.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.Room)
                .WithMany(r => r.Bookings)
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // CHECK-IN
        // =========================================================

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


        // =========================================================
        // NOTIFICATION
        // =========================================================

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");

            entity.HasKey(n => n.NotificationId);

            entity.Property(n => n.NotificationId)
                .HasColumnName("notificationid")
                .ValueGeneratedOnAdd();

            // -----------------------------------------------------
            // EMPLOYEE
            // -----------------------------------------------------

            entity.Property(n => n.EmployeeId)
                .HasColumnName("employeeid");

            entity.HasOne(n => n.Employee)
                .WithMany()
                .HasForeignKey(n => n.EmployeeId)
                .OnDelete(DeleteBehavior.NoAction);


            // -----------------------------------------------------
            // NORMAL BOOKING
            // -----------------------------------------------------

            entity.Property(n => n.BookingId)
                .HasColumnName("bookingid");

            entity.HasOne(n => n.Booking)
                .WithMany()
                .HasForeignKey(n => n.BookingId)
                .OnDelete(DeleteBehavior.NoAction);


            // -----------------------------------------------------
            // HOTSEAT BOOKING
            // -----------------------------------------------------

            entity.Property(n => n.HotseatBookingId)
                .HasColumnName("hotseatbookingid");

            entity.HasOne(n => n.HotseatBooking)
                .WithMany()
                .HasForeignKey(n => n.HotseatBookingId)
                .OnDelete(DeleteBehavior.Restrict);


            // -----------------------------------------------------
            // MESSAGE
            // -----------------------------------------------------

            entity.Property(n => n.Message)
                .HasColumnName("message")
                .HasMaxLength(500)
                .IsRequired();


            // -----------------------------------------------------
            // READ STATUS
            // -----------------------------------------------------

            entity.Property(n => n.IsRead)
                .HasColumnName("isread")
                .HasDefaultValue(false);


            // -----------------------------------------------------
            // CREATED DATE
            // -----------------------------------------------------

            entity.Property(n => n.CreatedAt)
                .HasColumnName("createdat")
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });


        // =========================================================
        // HOTSEAT BOOKING
        // =========================================================

        modelBuilder.Entity<HotseatBooking>(entity =>
        {
            entity.ToTable("hotseatbookings");

            entity.HasKey(x => x.HotseatBookingId);

            entity.Property(x => x.HotseatBookingId)
                .HasColumnName("hotseatbookingid");

            entity.Property(x => x.SeatId)
                .HasColumnName("seatid");

            entity.Property(x => x.EmployeeId)
                .HasColumnName("employeeid");

            entity.Property(x => x.BookingDate)
                .HasColumnName("bookingdate");

            entity.Property(x => x.BookingStatus)
                .HasColumnName("bookingstatus");

            // -----------------------------------------------------
            // UTC TIMESTAMP FIELDS
            // -----------------------------------------------------

            entity.Property(x => x.BookedOn)
                .HasColumnName("bookedon")
                .HasColumnType("timestamp with time zone");

            entity.Property(x => x.CheckInDeadline)
                .HasColumnName("checkindeadline")
                .HasColumnType("timestamp with time zone");

            entity.Property(x => x.CheckInTime)
                .HasColumnName("checkintime")
                .HasColumnType("timestamp with time zone");

            entity.Property(x => x.ReleasedOn)
                .HasColumnName("releasedon")
                .HasColumnType("timestamp with time zone");

            entity.Property(x => x.RecordIngestedBy)
                .HasColumnName("recordingestedby");

            entity.Property(x => x.RecordIngestedOn)
                .HasColumnName("recordingestedon")
                .HasColumnType("timestamp with time zone");

            entity.Property(x => x.RecordModifiedBy)
                .HasColumnName("recordmodifiedby");

            entity.Property(x => x.RecordModifiedOn)
                .HasColumnName("recordmodifiedon")
                .HasColumnType("timestamp with time zone");


            // -----------------------------------------------------
            // SEAT RELATIONSHIP
            // -----------------------------------------------------

            entity.HasOne(x => x.Seat)
                .WithMany(x => x.HotseatBookings)
                .HasForeignKey(x => x.SeatId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}