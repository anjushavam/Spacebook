using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Infrastructure.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("rooms");

        builder.HasKey(x => x.RoomId);

        builder.Property(x => x.RoomId)
               .HasColumnName("roomid");

        builder.Property(x => x.RoomTypeId)
               .HasColumnName("roomtypeid");

        builder.Property(x => x.RoomName)
               .HasColumnName("roomname")
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(x => x.Capacity)
               .HasColumnName("capacity")
               .IsRequired();

        builder.Property(x => x.Module)
               .HasColumnName("module")
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(x => x.Status)
               .HasColumnName("status")
               .HasMaxLength(20)
               .IsRequired();

        builder.HasOne(x => x.RoomType)
               .WithMany(x => x.Rooms)
               .HasForeignKey(x => x.RoomTypeId);

        builder.HasMany(x => x.RoomFacilities)
               .WithOne(x => x.Room)
               .HasForeignKey(x => x.RoomId);

        builder.HasMany(x => x.Bookings)
               .WithOne(x => x.Room)
               .HasForeignKey(x => x.RoomId);
       builder.Property(r => r.IsBlocked)
       .HasColumnName("isblocked");
    }
}