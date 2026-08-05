using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Infrastructure.Configurations;

public class RoomFacilityConfiguration : IEntityTypeConfiguration<RoomFacility>
{
    public void Configure(EntityTypeBuilder<RoomFacility> builder)
    {
        builder.ToTable("roomfacilities");

        builder.HasKey(x => x.RoomFacilityId);

        builder.Property(x => x.RoomFacilityId)
               .HasColumnName("roomfacilityid");

        builder.Property(x => x.RoomId)
               .HasColumnName("roomid");

        builder.Property(x => x.FacilityId)
               .HasColumnName("facilityid");

        builder.HasIndex(x => new { x.RoomId, x.FacilityId })
               .IsUnique();

        builder.HasOne(x => x.Room)
               .WithMany(x => x.RoomFacilities)
               .HasForeignKey(x => x.RoomId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Facility)
               .WithMany(x => x.RoomFacilities)
               .HasForeignKey(x => x.FacilityId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}