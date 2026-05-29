namespace TaskManagement.Application.DTOs;

public record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string CreatedByEmail);

public record CreateTaskRequest(string Title, string? Description);

public record UpdateTaskRequest(string Title, string? Description);

public record TaskSummaryDto(
    Guid Id,
    string Title,
    bool IsCompleted,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string CreatedByEmail);
