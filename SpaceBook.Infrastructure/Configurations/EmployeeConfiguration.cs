using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceBook.Domain.Entities;
 
namespace SpaceBook.Infrastructure.Configurations;
 
public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");
 
        builder.HasKey(x => x.EmployeeId);
 
        builder.Property(x => x.EmployeeId)
               .HasColumnName("employeeid");
 
        builder.Property(x => x.RoleId)
               .HasColumnName("roleid");
 
        builder.Property(x => x.Name)
               .HasColumnName("name");
 
        builder.Property(x => x.Email)
               .HasColumnName("email");
 
        builder.Property(x => x.PasswordHash)
               .HasColumnName("passwordhash");
 
        builder.Property(x => x.Department)
               .HasColumnName("department");
 
        builder.Property(x => x.IsActive)
               .HasColumnName("isactive");
 
        builder.Property(x => x.CreatedOn)
               .HasColumnName("createdon");
 
        builder.HasOne(x => x.Role)
               .WithMany(r => r.Employees)
               .HasForeignKey(x => x.RoleId);
    }
}