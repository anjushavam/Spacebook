namespace SpaceBook.Domain.Entities;
 
public class Employee
{
    public int EmployeeId { get; set; }
 
    public int RoleId { get; set; }
 
    public string Name { get; set; } = string.Empty;
 
    public string Email { get; set; } = string.Empty;
 
    public string PasswordHash { get; set; } = string.Empty;
 
    public string Department { get; set; } = string.Empty;
 
    public bool IsActive { get; set; }
 
    public DateTime CreatedOn { get; set; }
 
    public Role? Role { get; set; }
 
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}