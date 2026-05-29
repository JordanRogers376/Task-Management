using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user, string tenantName);
}
