namespace SpaceBook.Domain.Entities;

public class Role
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}