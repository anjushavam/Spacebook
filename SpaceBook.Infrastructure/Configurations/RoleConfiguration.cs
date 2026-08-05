using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceBook.Domain.Entities;
 
namespace SpaceBook.Infrastructure.Configurations;
 
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
 
        builder.HasKey(x => x.RoleId);
 
        builder.Property(x => x.RoleId)
               .HasColumnName("roleid");
 
        builder.Property(x => x.RoleName)
               .HasColumnName("rolename")
               .HasMaxLength(50)
               .IsRequired();
    }
}