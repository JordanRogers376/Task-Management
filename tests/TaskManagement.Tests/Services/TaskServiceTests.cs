using FluentAssertions;
using Moq;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Tests.Services;

public class TaskServiceTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<ITaskRepository> _taskRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    public TaskServiceTests()
    {
        _currentUser.Setup(c => c.TenantId).Returns(_tenantId);
        _currentUser.Setup(c => c.UserId).Returns(_userId);
        _currentUser.Setup(c => c.Role).Returns(UserRole.User);
    }

    private TaskService CreateSut() => new(_taskRepository.Object, _currentUser.Object);

    [Fact]
    public async Task CreateTaskAsync_AddsTaskForCurrentTenant()
    {
        _taskRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var createdTask = new TaskItem
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            CreatedByUserId = _userId,
            Title = "New task",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = new User { Email = "user@test.com" }
        };

        _taskRepository.Setup(r => r.GetByIdAsync(_tenantId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdTask);

        var sut = CreateSut();
        var result = await sut.CreateTaskAsync(new CreateTaskRequest("New task", null));

        result.Title.Should().Be("New task");
        _taskRepository.Verify(r => r.AddAsync(It.Is<TaskItem>(t =>
            t.TenantId == _tenantId && t.CreatedByUserId == _userId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteTaskAsync_SetsCompletedFlag()
    {
        var taskId = Guid.NewGuid();
        var task = new TaskItem
        {
            Id = taskId,
            TenantId = _tenantId,
            CreatedByUserId = _userId,
            Title = "Task",
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = new User { Email = "user@test.com" }
        };

        _taskRepository.Setup(r => r.GetByIdAsync(_tenantId, taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid id, CancellationToken _) =>
            {
                if (task.IsCompleted)
                    task.CompletedAt = DateTime.UtcNow;
                return task;
            });

        _taskRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.CompleteTaskAsync(taskId);

        result.IsCompleted.Should().BeTrue();
        _taskRepository.Verify(r => r.Update(It.Is<TaskItem>(t => t.IsCompleted)), Times.Once);
    }

    [Fact]
    public async Task UpdateTaskAsync_WhenUserDidNotCreateTask_ThrowsForbidden()
    {
        var taskId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        _taskRepository.Setup(r => r.GetByIdAsync(_tenantId, taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskItem
            {
                Id = taskId,
                TenantId = _tenantId,
                CreatedByUserId = otherUserId,
                Title = "Task",
                CreatedBy = new User { Email = "other@test.com" }
            });

        var sut = CreateSut();
        var act = () => sut.UpdateTaskAsync(taskId, new UpdateTaskRequest("Updated", null));

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task DeleteTaskAsync_WhenUserIsNotAdmin_ThrowsForbidden()
    {
        var sut = CreateSut();
        var act = () => sut.DeleteTaskAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task DeleteTaskAsync_WhenAdmin_RemovesTask()
    {
        _currentUser.Setup(c => c.Role).Returns(UserRole.Admin);
        var taskId = Guid.NewGuid();
        var task = new TaskItem { Id = taskId, TenantId = _tenantId, CreatedByUserId = _userId, Title = "Task" };

        _taskRepository.Setup(r => r.GetByIdAsync(_tenantId, taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _taskRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.DeleteTaskAsync(taskId);

        _taskRepository.Verify(r => r.Remove(task), Times.Once);
    }
}
