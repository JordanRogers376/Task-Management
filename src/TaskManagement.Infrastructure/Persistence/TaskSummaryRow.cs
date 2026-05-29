namespace TaskManagement.Infrastructure.Persistence;

public class TaskSummaryRow
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string CreatedByEmail { get; set; } = string.Empty;
}
