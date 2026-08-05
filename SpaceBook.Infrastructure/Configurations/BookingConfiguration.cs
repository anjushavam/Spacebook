using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Infrastructure.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");

        builder.HasKey(x => x.BookingId);

        builder.Property(x => x.BookingId)
               .HasColumnName("bookingid");

        builder.Property(x => x.RoomId)
               .HasColumnName("roomid");

        builder.Property(x => x.EmployeeId)
               .HasColumnName("employeeid");

        builder.Property(x => x.Purpose)
               .HasColumnName("purpose")
               .HasMaxLength(255)
               .IsRequired();

        builder.Property(x => x.ParticipantCount)
               .HasColumnName("participantcount")
               .IsRequired();

        builder.Property(x => x.BookingDate)
               .HasColumnName("bookingdate")
               .IsRequired();

        builder.Property(x => x.StartTime)
               .HasColumnName("starttime")
               .IsRequired();

        builder.Property(x => x.EndTime)
               .HasColumnName("endtime")
               .IsRequired();

        builder.Property(x => x.BookedOn)
               .HasColumnName("bookedon")
               .IsRequired();

        builder.Property(x => x.Status)
               .HasColumnName("status")
               .HasMaxLength(20)
               .IsRequired();

        builder.HasOne(x => x.Room)
               .WithMany(x => x.Bookings)
               .HasForeignKey(x => x.RoomId);

        builder.HasOne(x => x.Employee)
               .WithMany(x => x.Bookings)
               .HasForeignKey(x => x.EmployeeId);
    }
}