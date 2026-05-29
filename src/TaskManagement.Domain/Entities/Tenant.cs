namespace TaskManagement.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
