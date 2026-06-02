using AutoMapper;
using FluentAssertions;
using Moq;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Mapping;
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
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly IMapper _mapper;

    public TaskServiceTests()
    {
        _currentUser.Setup(c => c.TenantId).Returns(_tenantId);
        _currentUser.Setup(c => c.UserId).Returns(_userId);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Admin);

        _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
    }

    private TaskService CreateSut() =>
        new(_taskRepository.Object, _userRepository.Object, _currentUser.Object, _mapper);

    #region CreateTask Tests

    [Fact]
    public async Task CreateTaskAsync_AddsTaskWithCorrectTenantId()
    {
        // Arrange
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = _userId, TenantId = _tenantId, Username = "admin" });

        _taskRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var createdTask = new TaskItem
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            AssignedUserId = _userId,
            Title = "New task",
            CreatedDate = DateTime.UtcNow,
            AssignedUser = new User { Username = "admin" }
        };

        _taskRepository.Setup(r => r.GetByIdAsync(_tenantId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdTask);

        var sut = CreateSut();

        // Act
        var result = await sut.CreateTaskAsync(new CreateTaskRequest("New task", "Description", null));

        // Assert
        result.Title.Should().Be("New task");
        _taskRepository.Verify(r => r.AddAsync(
            It.Is<TaskItem>(t => t.TenantId == _tenantId && t.Title == "New task"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTaskAsync_AssignsToCurrentUserByDefault()
    {
        // Arrange
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = _userId, TenantId = _tenantId, Username = "admin" });

        _taskRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _taskRepository.Setup(r => r.GetByIdAsync(_tenantId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskItem
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                AssignedUserId = _userId,
                Title = "Task",
                AssignedUser = new User { Username = "admin" }
            });

        var sut = CreateSut();

        // Act
        await sut.CreateTaskAsync(new CreateTaskRequest("Task", null, null));

        // Assert - task is assigned to current user when no assignee specified
        _taskRepository.Verify(r => r.AddAsync(
            It.Is<TaskItem>(t => t.AssignedUserId == _userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CompleteTask Tests

    [Fact]
    public async Task CompleteTaskAsync_SetsIsCompletedToTrue()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = new TaskItem
        {
            Id = taskId,
            TenantId = _tenantId,
            AssignedUserId = _userId,
            Title = "Task",
            IsCompleted = false,
            CreatedDate = DateTime.UtcNow,
            AssignedUser = new User { Username = "user" }
        };

        _taskRepository.Setup(r => r.GetByIdAsync(_tenantId, taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _taskRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        var result = await sut.CompleteTaskAsync(taskId);

        // Assert
        result.IsCompleted.Should().BeTrue();
        _taskRepository.Verify(r => r.Update(It.Is<TaskItem>(t => t.IsCompleted && t.CompletedAt != null)), Times.Once);
    }

    [Fact]
    public async Task CompleteTaskAsync_WhenUserNotAssigned_ThrowsForbiddenException()
    {
        // Arrange - user (not admin) tries to complete a task assigned to someone else
        _currentUser.Setup(c => c.Role).Returns(UserRole.User);
        var otherUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _taskRepository.Setup(r => r.GetByIdAsync(_tenantId, taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskItem
            {
                Id = taskId,
                TenantId = _tenantId,
                AssignedUserId = otherUserId, // Different user
                Title = "Task",
                AssignedUser = new User { Username = "other" }
            });

        var sut = CreateSut();

        // Act
        var act = () => sut.CompleteTaskAsync(taskId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*only complete tasks assigned to you*");
    }

    [Fact]
    public async Task CompleteTaskAsync_AdminCanCompleteAnyTask()
    {
        // Arrange - admin completes task assigned to someone else
        _currentUser.Setup(c => c.Role).Returns(UserRole.Admin);
        var otherUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var task = new TaskItem
        {
            Id = taskId,
            TenantId = _tenantId,
            AssignedUserId = otherUserId, // Different user
            Title = "Task",
            IsCompleted = false,
            AssignedUser = new User { Username = "other" }
        };

        _taskRepository.Setup(r => r.GetByIdAsync(_tenantId, taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _taskRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        var result = await sut.CompleteTaskAsync(taskId);

        // Assert - admin can complete any task
        result.IsCompleted.Should().BeTrue();
    }

    #endregion

    #region UpdateTask Tests

    [Fact]
    public async Task UpdateTaskAsync_UpdatesTitleAndDescription()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = new TaskItem
        {
            Id = taskId,
            TenantId = _tenantId,
            AssignedUserId = _userId,
            Title = "Old Title",
            Description = "Old Description",
            AssignedUser = new User { Username = "user" }
        };

        _taskRepository.Setup(r => r.GetByIdAsync(_tenantId, taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _taskRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateTaskAsync(taskId, new UpdateTaskRequest("New Title", "New Description", null));

        // Assert
        result.Title.Should().Be("New Title");
        result.Description.Should().Be("New Description");
        _taskRepository.Verify(r => r.Update(It.Is<TaskItem>(t =>
            t.Title == "New Title" && t.Description == "New Description")), Times.Once);
    }

    #endregion

    #region Tenant Isolation Tests

    [Fact]
    public async Task GetTaskAsync_WhenTaskBelongsToDifferentTenant_ThrowsNotFoundException()
    {
        // Arrange - task exists but belongs to different tenant
        var taskId = Guid.NewGuid();

        // Repository returns null because it filters by tenant
        _taskRepository.Setup(r => r.GetByIdAsync(_tenantId, taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        var sut = CreateSut();

        // Act
        var act = () => sut.GetTaskAsync(taskId);

        // Assert - tenant A cannot see tenant B's task
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetTasksForTenantAsync_OnlyReturnsTasksForCurrentTenant()
    {
        // Arrange
        var tenantATasks = new List<TaskItem>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                AssignedUserId = _userId,
                Title = "Tenant A Task 1",
                CreatedDate = DateTime.UtcNow,
                AssignedUser = new User { Username = "user" }
            },
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                AssignedUserId = _userId,
                Title = "Tenant A Task 2",
                CreatedDate = DateTime.UtcNow,
                AssignedUser = new User { Username = "user" }
            }
        };

        // Repository only returns tasks for the current tenant
        _taskRepository.Setup(r => r.GetByTenantAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantATasks);

        var sut = CreateSut();

        // Act
        var result = await sut.GetTasksForTenantAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.Title.StartsWith("Tenant A"));

        // Verify repository was called with correct tenant ID
        _taskRepository.Verify(r => r.GetByTenantAsync(_tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTaskAsync_WhenTaskBelongsToDifferentTenant_ThrowsNotFoundException()
    {
        // Arrange
        var taskId = Guid.NewGuid();

        // Task doesn't exist for this tenant (filtered by repository)
        _taskRepository.Setup(r => r.GetByIdAsync(_tenantId, taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        var sut = CreateSut();

        // Act
        var act = () => sut.DeleteTaskAsync(taskId);

        // Assert - cannot delete task from another tenant
        await act.Should().ThrowAsync<NotFoundException>();
    }

    #endregion
}
