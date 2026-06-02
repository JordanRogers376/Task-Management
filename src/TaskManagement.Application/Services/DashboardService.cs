using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Application.Services;

public class DashboardService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUser;

    public DashboardService(ITaskRepository taskRepository, ICurrentUserService currentUser)
    {
        _taskRepository = taskRepository;
        _currentUser = currentUser;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _taskRepository.GetTenantTaskSummaryAsync(_currentUser.TenantId, cancellationToken);
        return new DashboardSummaryDto(summary.TotalTasks, summary.CompletedTasks, summary.PendingTasks);
    }
}
