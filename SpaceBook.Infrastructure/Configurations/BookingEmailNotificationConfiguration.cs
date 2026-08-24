using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Infrastructure.Configurations;

public class BookingEmailNotificationConfiguration : IEntityTypeConfiguration<BookingEmailNotification>
{
    public void Configure(EntityTypeBuilder<BookingEmailNotification> builder)
    {
        builder.ToTable("bookingemailnotifications");

        builder.HasKey(x => x.BookingEmailNotificationId);

        builder.Property(x => x.BookingEmailNotificationId)
               .HasColumnName("bookingemailnotificationid");

        builder.Property(x => x.BookingId)
               .HasColumnName("bookingid")
               .IsRequired();

        builder.Property(x => x.NotificationType)
               .HasColumnName("notificationtype")
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(x => x.SentAt)
               .HasColumnName("sentat")
               .HasColumnType("timestamp with time zone")
               .IsRequired();

        builder.Property(x => x.Status)
               .HasColumnName("status")
               .HasMaxLength(50)
               .IsRequired();

        // Unique constraint to prevent duplicate emails for a booking + notification type
        builder.HasIndex(x => new { x.BookingId, x.NotificationType })
               .IsUnique()
               .HasDatabaseName("ix_bookingemailnotifications_bookingid_notificationtype");

        builder.HasOne(x => x.Booking)
               .WithMany(x => x.EmailNotifications)
               .HasForeignKey(x => x.BookingId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
