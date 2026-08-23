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

    public DbSet<RoomFacility> RoomFacilities =>
        Set<RoomFacility>();

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<CheckIn> CheckIns => Set<CheckIn>();

    public DbSet<Notification> Notifications =>
        Set<Notification>();

    public DbSet<HotseatBooking> HotseatBookings =>
        Set<HotseatBooking>();

    public DbSet<Seat> Seats => Set<Seat>();

    public DbSet<Office> Offices => Set<Office>();

    public DbSet<Location> Locations => Set<Location>();


    // =========================================================
    // MODEL CONFIGURATION
    // =========================================================

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        // =====================================================
        // ROLE
        // =====================================================

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");

            entity.HasKey(x => x.RoleId);

            entity.Property(x => x.RoleId)
                .HasColumnName("roleid");

            entity.Property(x => x.RoleName)
                .HasColumnName("rolename")
                .HasMaxLength(50)
                .IsRequired();

            entity.HasMany(x => x.Employees)
                .WithOne(x => x.Role)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =====================================================
        // EMPLOYEE
        // =====================================================

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("employees");

            entity.HasKey(x => x.EmployeeId);

            entity.Property(x => x.EmployeeId)
                .HasColumnName("employeeid");

            entity.Property(x => x.RoleId)
                .HasColumnName("roleid");

            entity.Property(x => x.Name)
                .HasColumnName("name");

            entity.Property(x => x.Email)
                .HasColumnName("email");

            entity.Property(x => x.PasswordHash)
                .HasColumnName("passwordhash");

            entity.Property(x => x.Department)
                .HasColumnName("department");

            entity.Property(x => x.IsActive)
                .HasColumnName("isactive");

            entity.Property(x => x.CreatedOn)
                .HasColumnName("createdon")
                .HasColumnType(
                    "timestamp with time zone");

            entity.HasOne(x => x.Role)
                .WithMany(x => x.Employees)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Bookings)
                .WithOne(x => x.Employee)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =====================================================
        // ROOM TYPE
        // =====================================================

        modelBuilder.Entity<RoomType>(entity =>
        {
            entity.ToTable("roomtypes");

            entity.HasKey(x => x.RoomTypeId);

            entity.Property(x => x.RoomTypeId)
                .HasColumnName("roomtypeid");

            entity.Property(x => x.TypeName)
                .HasColumnName("typename")
                .HasMaxLength(100)
                .IsRequired();

            entity.HasMany(x => x.Rooms)
                .WithOne(x => x.RoomType)
                .HasForeignKey(x => x.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =====================================================
        // LOCATION
        // =====================================================

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


        // =====================================================
        // OFFICE
        // =====================================================

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


        // =====================================================
        // MODULE
        // =====================================================

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
                .HasColumnName("recordingestedon")
                .HasColumnType(
                    "timestamp with time zone");

            entity.Property(x => x.RecordModifiedBy)
                .HasColumnName("recordmodifiedby");

            entity.Property(x => x.RecordModifiedOn)
                .HasColumnName("recordmodifiedon")
                .HasColumnType(
                    "timestamp with time zone");

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


        // =====================================================
        // SEAT
        // =====================================================

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


        // =====================================================
        // ROOM
        // =====================================================

        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("rooms");

            entity.HasKey(x => x.RoomId);

            entity.Property(x => x.RoomId)
                .HasColumnName("roomid");

            entity.Property(x => x.RoomNumber)
                .HasColumnName("roomnumber")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.RoomTypeId)
                .HasColumnName("roomtypeid");

            entity.Property(x => x.ModuleId)
                .HasColumnName("moduleid");

            entity.Property(x => x.RoomName)
                .HasColumnName("roomname");

            entity.Property(x => x.Capacity)
                .HasColumnName("capacity");

            entity.Property(x => x.Status)
                .HasColumnName("status");

            entity.Property(x => x.IsBlocked)
                .HasColumnName("isblocked");

            entity.HasOne(x => x.RoomType)
                .WithMany(x => x.Rooms)
                .HasForeignKey(x => x.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Module)
                .WithMany(x => x.Rooms)
                .HasForeignKey(x => x.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.RoomFacilities)
                .WithOne(x => x.Room)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.Bookings)
                .WithOne(x => x.Room)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =====================================================
        // FACILITY
        // =====================================================

        modelBuilder.Entity<Facility>(entity =>
        {
            entity.ToTable("facilities");

            entity.HasKey(x => x.FacilityId);

            entity.Property(x => x.FacilityId)
                .HasColumnName("facilityid");

            entity.Property(x => x.FacilityName)
                .HasColumnName("facilityname")
                .HasMaxLength(100)
                .IsRequired();

            entity.HasMany(x => x.RoomFacilities)
                .WithOne(x => x.Facility)
                .HasForeignKey(x => x.FacilityId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        // =====================================================
        // ROOM FACILITY
        // =====================================================

        modelBuilder.Entity<RoomFacility>(entity =>
        {
            entity.ToTable("roomfacilities");

            entity.HasKey(x => x.RoomFacilityId);

            entity.Property(x => x.RoomFacilityId)
                .HasColumnName("roomfacilityid");

            entity.Property(x => x.RoomId)
                .HasColumnName("roomid");

            entity.Property(x => x.FacilityId)
                .HasColumnName("facilityid");

            entity.HasOne(x => x.Room)
                .WithMany(x => x.RoomFacilities)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Facility)
                .WithMany(x => x.RoomFacilities)
                .HasForeignKey(x => x.FacilityId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        // =====================================================
        // BOOKING
        // =====================================================

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("bookings");

            entity.HasKey(x => x.BookingId);

            entity.Property(x => x.BookingId)
                .HasColumnName("bookingid")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.RoomId)
                .HasColumnName("roomid")
                .IsRequired();

            entity.Property(x => x.EmployeeId)
                .HasColumnName("employeeid")
                .IsRequired();

            entity.Property(x => x.MeetingTitle)
                .HasColumnName("meetingtitle")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Purpose)
                .HasColumnName("purpose")
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.ParticipantCount)
                .HasColumnName("participantcount")
                .IsRequired();

            entity.Property(x => x.BookingDate)
                .HasColumnName("bookingdate")
                .IsRequired();

            entity.Property(x => x.StartTime)
                .HasColumnName("starttime")
                .IsRequired();

            entity.Property(x => x.EndTime)
                .HasColumnName("endtime")
                .IsRequired();

            entity.Property(x => x.BookedOn)
                .HasColumnName("bookedon")
                .HasColumnType(
                    "timestamp with time zone")
                .IsRequired();

            entity.Property(x => x.Status)
                .HasColumnName("status")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.CancellationReason)
                .HasColumnName("cancellationreason")
                .HasMaxLength(500);

            entity.Property(x => x.StartReminderSent)
                .HasColumnName("startremindersent")
                .HasDefaultValue(false)
                .IsRequired();

            entity.Property(x => x.EndReminderSent)
                .HasColumnName("endremindersent")
                .HasDefaultValue(false)
                .IsRequired();

            // -------------------------------------------------
            // EMPLOYEE
            // -------------------------------------------------

            entity.HasOne(x => x.Employee)
                .WithMany(x => x.Bookings)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // -------------------------------------------------
            // ROOM
            // -------------------------------------------------

            entity.HasOne(x => x.Room)
                .WithMany(x => x.Bookings)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // -------------------------------------------------
            // CHECK-IN
            // -------------------------------------------------

            entity.HasOne(x => x.CheckIn)
                .WithOne(x => x.Booking)
                .HasForeignKey<CheckIn>(
                    x => x.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        // =====================================================
        // CHECK-IN
        // =====================================================

        modelBuilder.Entity<CheckIn>(entity =>
        {
            entity.ToTable("checkins");

            entity.HasKey(x => x.CheckInId);

            entity.Property(x => x.CheckInId)
                .HasColumnName("checkinid")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.BookingId)
                .HasColumnName("bookingid")
                .IsRequired();

            entity.Property(x => x.CheckedInAt)
                .HasColumnName("checkedinat")
                .HasColumnType(
                    "timestamp with time zone")
                .IsRequired();

            entity.Property(x => x.Status)
                .HasColumnName("status")
                .HasMaxLength(50)
                .IsRequired();

            entity.HasOne(x => x.Booking)
                .WithOne(x => x.CheckIn)
                .HasForeignKey<CheckIn>(
                    x => x.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.BookingId)
                .IsUnique();
        });


        // =====================================================
        // NOTIFICATION
        // =====================================================

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");

            // -------------------------------------------------
            // PRIMARY KEY
            // -------------------------------------------------

            entity.HasKey(x =>
                x.NotificationId);

            entity.Property(x =>
                    x.NotificationId)
                .HasColumnName(
                    "notificationid")
                .ValueGeneratedOnAdd();


            // -------------------------------------------------
            // EMPLOYEE
            // -------------------------------------------------

            entity.Property(x =>
                    x.EmployeeId)
                .HasColumnName(
                    "employeeid")
                .IsRequired();


            // -------------------------------------------------
            // NORMAL ROOM BOOKING
            // Nullable because hotseat notifications will not
            // have a normal BookingId.
            // -------------------------------------------------

            entity.Property(x =>
                    x.BookingId)
                .HasColumnName(
                    "bookingid")
                .IsRequired(false);


            // -------------------------------------------------
            // HOTSEAT BOOKING
            // Nullable because room notifications will not
            // have a HotseatBookingId.
            // -------------------------------------------------

            entity.Property(x =>
                    x.HotseatBookingId)
                .HasColumnName(
                    "hotseatbookingid")
                .IsRequired(false);


            // -------------------------------------------------
            // MESSAGE
            // -------------------------------------------------

            entity.Property(x =>
                    x.Message)
                .HasColumnName(
                    "message")
                .HasMaxLength(500)
                .IsRequired();


            // -------------------------------------------------
            // READ STATUS
            // -------------------------------------------------

            entity.Property(x =>
                    x.IsRead)
                .HasColumnName(
                    "isread")
                .HasDefaultValue(false)
                .IsRequired();


            // -------------------------------------------------
            // CREATED DATE
            //
            // PostgreSQL:
            // timestamp with time zone
            //
            // Application code should use:
            // DateTime.UtcNow
            // -------------------------------------------------

            entity.Property(x =>
                    x.CreatedAt)
                .HasColumnName(
                    "createdat")
                .HasColumnType(
                    "timestamp with time zone")
                .HasDefaultValueSql(
                    "CURRENT_TIMESTAMP")
                .IsRequired();


            // -------------------------------------------------
            // EMPLOYEE RELATIONSHIP
            // -------------------------------------------------

            entity.HasOne(x =>
                    x.Employee)
                .WithMany()
                .HasForeignKey(x =>
                    x.EmployeeId)
                .OnDelete(
                    DeleteBehavior.Restrict);


            // -------------------------------------------------
            // NORMAL ROOM BOOKING RELATIONSHIP
            //
            // SetNull keeps notification history even when a
            // normal booking is deleted.
            // -------------------------------------------------

            entity.HasOne(x =>
                    x.Booking)
                .WithMany()
                .HasForeignKey(x =>
                    x.BookingId)
                .OnDelete(
                    DeleteBehavior.SetNull);


            // -------------------------------------------------
            // HOTSEAT BOOKING RELATIONSHIP
            //
            // SetNull keeps notification history even when a
            // hotseat booking is deleted.
            // -------------------------------------------------

            entity.HasOne(x =>
                    x.HotseatBooking)
                .WithMany()
                .HasForeignKey(x =>
                    x.HotseatBookingId)
                .OnDelete(
                    DeleteBehavior.SetNull);


            // -------------------------------------------------
            // INDEXES
            // -------------------------------------------------

            entity.HasIndex(x =>
                x.EmployeeId);

            entity.HasIndex(x =>
                x.BookingId);

            entity.HasIndex(x =>
                x.HotseatBookingId);

            entity.HasIndex(x =>
                x.CreatedAt);

            entity.HasIndex(x =>
                x.IsRead);
        });


        // =====================================================
        // HOTSEAT BOOKING
        // =====================================================

        modelBuilder.Entity<HotseatBooking>(entity =>
        {
            entity.ToTable(
                "hotseatbookings");

            entity.HasKey(x =>
                x.HotseatBookingId);

            entity.Property(x =>
                    x.HotseatBookingId)
                .HasColumnName(
                    "hotseatbookingid")
                .ValueGeneratedOnAdd();

            entity.Property(x =>
                    x.SeatId)
                .HasColumnName(
                    "seatid")
                .IsRequired();

            entity.Property(x =>
                    x.EmployeeId)
                .HasColumnName(
                    "employeeid")
                .IsRequired();

            entity.Property(x =>
                    x.BookingDate)
                .HasColumnName(
                    "bookingdate")
                .IsRequired();

            entity.Property(x =>
                    x.BookingStatus)
                .HasColumnName(
                    "bookingstatus")
                .HasMaxLength(50)
                .IsRequired();


            // -------------------------------------------------
            // TIMESTAMPS
            // PostgreSQL timestamp with time zone
            // -------------------------------------------------

            entity.Property(x =>
                    x.BookedOn)
                .HasColumnName(
                    "bookedon")
                .HasColumnType(
                    "timestamp with time zone");

            entity.Property(x =>
                    x.CheckInDeadline)
                .HasColumnName(
                    "checkindeadline")
                .HasColumnType(
                    "timestamp with time zone");

            entity.Property(x =>
                    x.CheckInTime)
                .HasColumnName(
                    "checkintime")
                .HasColumnType(
                    "timestamp with time zone");

            entity.Property(x =>
                    x.ReleasedOn)
                .HasColumnName(
                    "releasedon")
                .HasColumnType(
                    "timestamp with time zone");

            entity.Property(x =>
                    x.RecordIngestedBy)
                .HasColumnName(
                    "recordingestedby");

            entity.Property(x =>
                    x.RecordIngestedOn)
                .HasColumnName(
                    "recordingestedon")
                .HasColumnType(
                    "timestamp with time zone");

            entity.Property(x =>
                    x.RecordModifiedBy)
                .HasColumnName(
                    "recordmodifiedby");

            entity.Property(x =>
                    x.RecordModifiedOn)
                .HasColumnName(
                    "recordmodifiedon")
                .HasColumnType(
                    "timestamp with time zone");


            // -------------------------------------------------
            // SEAT RELATIONSHIP
            // -------------------------------------------------

            entity.HasOne(x =>
                    x.Seat)
                .WithMany(x =>
                    x.HotseatBookings)
                .HasForeignKey(x =>
                    x.SeatId)
                .OnDelete(
                    DeleteBehavior.Restrict);


            // -------------------------------------------------
            // EMPLOYEE RELATIONSHIP
            // -------------------------------------------------

            entity.HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x =>
                    x.EmployeeId)
                .OnDelete(
                    DeleteBehavior.Restrict);


            // -------------------------------------------------
            // INDEXES
            // -------------------------------------------------

            entity.HasIndex(x => new
            {
                x.SeatId,
                x.BookingDate
            });

            entity.HasIndex(x => new
            {
                x.EmployeeId,
                x.BookingDate
            });
        });
    }
}