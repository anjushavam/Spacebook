using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Infrastructure.Configurations;

public class FacilityConfiguration : IEntityTypeConfiguration<Facility>
{
    public void Configure(EntityTypeBuilder<Facility> builder)
    {
        builder.ToTable("facilities");

        builder.HasKey(x => x.FacilityId);

        builder.Property(x => x.FacilityId)
               .HasColumnName("facilityid");

        builder.Property(x => x.FacilityName)
               .HasColumnName("facilityname")
               .HasMaxLength(100)
               .IsRequired();

        builder.HasIndex(x => x.FacilityName)
               .IsUnique();
    }
}