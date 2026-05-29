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
            .Include(t => t.CreatedBy)
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<TaskItem?> GetByIdAsync(Guid tenantId, Guid taskId, CancellationToken cancellationToken = default) =>
        await _context.Tasks
            .Include(t => t.CreatedBy)
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Id == taskId, cancellationToken);

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default) =>
        await _context.Tasks.AddAsync(task, cancellationToken);

    public void Update(TaskItem task) => _context.Tasks.Update(task);

    public void Remove(TaskItem task) => _context.Tasks.Remove(task);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public async Task<IReadOnlyList<TaskSummary>> GetTaskSummariesByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                t.Id,
                t.Title,
                t.IsCompleted,
                t.CreatedAt,
                t.CompletedAt,
                u.Email AS CreatedByEmail
            FROM Tasks t
            INNER JOIN Users u ON t.CreatedByUserId = u.Id
            WHERE t.TenantId = {0}
            ORDER BY t.IsCompleted ASC, t.CreatedAt DESC
            """;

        var rows = await _context.Set<TaskSummaryRow>()
            .FromSqlRaw(sql, tenantId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return rows.Select(r => new TaskSummary(
            r.Id,
            r.Title,
            r.IsCompleted,
            r.CreatedAt,
            r.CompletedAt,
            r.CreatedByEmail)).ToList();
    }
}
