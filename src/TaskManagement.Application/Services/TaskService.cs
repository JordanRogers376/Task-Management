using AutoMapper;
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
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public TaskService(
        ITaskRepository taskRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        _taskRepository = taskRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<TaskDto>> GetTasksForTenantAsync(CancellationToken cancellationToken = default)
    {
        var tasks = await _taskRepository.GetByTenantAsync(_currentUser.TenantId, cancellationToken);
        return tasks.Select(t => _mapper.Map<TaskDto>(t)).ToList();
    }

    public async Task<TaskDto> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await GetTaskForTenantAsync(taskId, cancellationToken);
        return _mapper.Map<TaskDto>(task);
    }

    public async Task<TaskDto> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var assignedUserId = request.AssignedUserId ?? _currentUser.UserId;
        await EnsureAssigneeInTenantAsync(assignedUserId, cancellationToken);

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUser.TenantId,
            AssignedUserId = assignedUserId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            IsCompleted = false,
            CreatedDate = DateTime.UtcNow
        };

        await _taskRepository.AddAsync(task, cancellationToken);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        var created = await _taskRepository.GetByIdAsync(_currentUser.TenantId, task.Id, cancellationToken);
        return _mapper.Map<TaskDto>(created!);
    }

    public async Task<TaskDto> UpdateTaskAsync(Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var task = await GetTaskForTenantAsync(taskId, cancellationToken);

        if (request.AssignedUserId.HasValue)
        {
            await EnsureAssigneeInTenantAsync(request.AssignedUserId.Value, cancellationToken);
            task.AssignedUserId = request.AssignedUserId.Value;
        }

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim();
        _taskRepository.Update(task);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        var updated = await _taskRepository.GetByIdAsync(_currentUser.TenantId, taskId, cancellationToken);
        return _mapper.Map<TaskDto>(updated!);
    }

    public async Task<TaskDto> CompleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await GetTaskForTenantAsync(taskId, cancellationToken);
        EnsureCanComplete(task);

        if (!task.IsCompleted)
        {
            task.IsCompleted = true;
            task.CompletedAt = DateTime.UtcNow;
            _taskRepository.Update(task);
            await _taskRepository.SaveChangesAsync(cancellationToken);
        }

        var updated = await _taskRepository.GetByIdAsync(_currentUser.TenantId, taskId, cancellationToken);
        return _mapper.Map<TaskDto>(updated!);
    }

    public async Task DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
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

    private void EnsureCanComplete(TaskItem task)
    {
        if (_currentUser.Role == UserRole.Admin)
            return;

        if (task.AssignedUserId != _currentUser.UserId)
            throw new ForbiddenException("You can only complete tasks assigned to you.");
    }

    private async Task EnsureAssigneeInTenantAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || user.TenantId != _currentUser.TenantId)
            throw new NotFoundException("Assigned user was not found in your tenant.");
    }
}
