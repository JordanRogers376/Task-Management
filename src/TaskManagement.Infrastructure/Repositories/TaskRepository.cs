using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public TaskRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<TaskItem>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        await _context.Tasks
            .AsNoTracking()
            .Include(t => t.AssignedUser)
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync(cancellationToken);

    public async Task<TaskItem?> GetByIdAsync(Guid tenantId, Guid taskId, CancellationToken cancellationToken = default) =>
        await _context.Tasks
            .Include(t => t.AssignedUser)
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Id == taskId, cancellationToken);

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default) =>
        await _context.Tasks.AddAsync(task, cancellationToken);

    public void Update(TaskItem task) => _context.Tasks.Update(task);

    public void Remove(TaskItem task) => _context.Tasks.Remove(task);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public async Task<TenantTaskSummary> GetTenantTaskSummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Tasks.AsNoTracking().Where(t => t.TenantId == tenantId);

        var total = await query.CountAsync(cancellationToken);
        var completed = await query.CountAsync(t => t.IsCompleted, cancellationToken);

        return new TenantTaskSummary(total, completed, total - completed);
    }
}
