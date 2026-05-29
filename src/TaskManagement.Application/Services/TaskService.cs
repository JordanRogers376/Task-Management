using TaskManagement.Application.DTOs;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Application.Services;

public class TaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUser;

    public TaskService(ITaskRepository taskRepository, ICurrentUserService currentUser)
    {
        _taskRepository = taskRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<TaskDto>> GetTasksAsync(CancellationToken cancellationToken = default)
    {
        var tasks = await _taskRepository.GetByTenantAsync(_currentUser.TenantId, cancellationToken);
        return tasks.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<TaskSummaryDto>> GetTaskSummariesAsync(CancellationToken cancellationToken = default)
    {
        var summaries = await _taskRepository.GetTaskSummariesByTenantAsync(_currentUser.TenantId, cancellationToken);
        return summaries.Select(s => new TaskSummaryDto(
            s.Id, s.Title, s.IsCompleted, s.CreatedAt, s.CompletedAt, s.CreatedByEmail)).ToList();
    }

    public async Task<TaskDto> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await GetTaskForTenantAsync(taskId, cancellationToken);
        return MapToDto(task);
    }

    public async Task<TaskDto> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUser.TenantId,
            CreatedByUserId = _currentUser.UserId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _taskRepository.AddAsync(task, cancellationToken);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        var created = await _taskRepository.GetByIdAsync(_currentUser.TenantId, task.Id, cancellationToken);
        return MapToDto(created!);
    }

    public async Task<TaskDto> UpdateTaskAsync(Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var task = await GetTaskForTenantAsync(taskId, cancellationToken);
        EnsureCanModify(task);

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim();
        _taskRepository.Update(task);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        var updated = await _taskRepository.GetByIdAsync(_currentUser.TenantId, taskId, cancellationToken);
        return MapToDto(updated!);
    }

    public async Task<TaskDto> CompleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await GetTaskForTenantAsync(taskId, cancellationToken);

        if (!task.IsCompleted)
        {
            task.IsCompleted = true;
            task.CompletedAt = DateTime.UtcNow;
            _taskRepository.Update(task);
            await _taskRepository.SaveChangesAsync(cancellationToken);
        }

        var updated = await _taskRepository.GetByIdAsync(_currentUser.TenantId, taskId, cancellationToken);
        return MapToDto(updated!);
    }

    public async Task DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        if (_currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("Only administrators can delete tasks.");

        var task = await GetTaskForTenantAsync(taskId, cancellationToken);
        _taskRepository.Remove(task);
        await _taskRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<TaskItem> GetTaskForTenantAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdAsync(_currentUser.TenantId, taskId, cancellationToken);
        if (task is null)
            throw new NotFoundException($"Task {taskId} was not found.");
        return task;
    }

    private void EnsureCanModify(TaskItem task)
    {
        if (_currentUser.Role == UserRole.Admin)
            return;

        if (task.CreatedByUserId != _currentUser.UserId)
            throw new ForbiddenException("You can only edit tasks you created.");
    }

    private static TaskDto MapToDto(TaskItem task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.IsCompleted,
        task.CreatedAt,
        task.CompletedAt,
        task.CreatedBy?.Email ?? string.Empty);
}
