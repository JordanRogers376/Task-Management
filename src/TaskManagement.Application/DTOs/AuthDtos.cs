namespace TaskManagement.Application.DTOs;

public record LoginRequest(string Username, string Password);

public record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    string Username,
    string Role,
    Guid TenantId,
    string TenantName);
