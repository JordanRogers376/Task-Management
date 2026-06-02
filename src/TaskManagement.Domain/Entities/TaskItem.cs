namespace TaskManagement.Domain.Entities;

public class TaskItem
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AssignedUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public User AssignedUser { get; set; } = null!;
}
