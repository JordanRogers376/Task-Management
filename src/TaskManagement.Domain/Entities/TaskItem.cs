namespace TaskManagement.Domain.Entities;

public class TaskItem
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
}
