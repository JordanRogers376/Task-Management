namespace TaskManagement.Application.DTOs;

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    string Email,
    string Role,
    Guid TenantId,
    string TenantName);
