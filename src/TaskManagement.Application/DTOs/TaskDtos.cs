namespace TaskManagement.Application.DTOs;

public record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTime CreatedDate,
    DateTime? CompletedAt,
    Guid AssignedUserId,
    string AssignedUsername);

public record CreateTaskRequest(string Title, string? Description, Guid? AssignedUserId);

public record UpdateTaskRequest(string Title, string? Description, Guid? AssignedUserId);

public record DashboardSummaryDto(int TotalTasks, int CompletedTasks, int PendingTasks);
