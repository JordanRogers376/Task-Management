using TaskManagement.Domain.Entities;

namespace TaskManagement.Domain.Interfaces;

public interface ITaskRepository
{
    Task<IReadOnlyList<TaskItem>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TaskItem?> GetByIdAsync(Guid tenantId, Guid taskId, CancellationToken cancellationToken = default);
    Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);
    void Update(TaskItem task);
    void Remove(TaskItem task);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<TenantTaskSummary> GetTenantTaskSummaryAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public record TenantTaskSummary(int TotalTasks, int CompletedTasks, int PendingTasks);
