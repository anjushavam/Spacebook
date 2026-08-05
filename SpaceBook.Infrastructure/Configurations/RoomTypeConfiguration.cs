using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Infrastructure.Configurations;

public class RoomTypeConfiguration : IEntityTypeConfiguration<RoomType>
{
    public void Configure(EntityTypeBuilder<RoomType> builder)
    {
        builder.ToTable("roomtypes");

        builder.HasKey(x => x.RoomTypeId);

        builder.Property(x => x.RoomTypeId)
               .HasColumnName("roomtypeid");

        builder.Property(x => x.TypeName)
               .HasColumnName("typename")
               .HasMaxLength(100)
               .IsRequired();

        builder.HasIndex(x => x.TypeName)
               .IsUnique();

        builder.HasMany(x => x.Rooms)
               .WithOne(x => x.RoomType)
               .HasForeignKey(x => x.RoomTypeId);
    }
}