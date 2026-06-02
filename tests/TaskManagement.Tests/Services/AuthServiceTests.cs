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

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();

    private AuthService CreateSut() =>
        new(_userRepository.Object, _passwordHasher.Object, _tokenService.Object);

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Username = "admin@acme.com",
            PasswordHash = "hash",
            Role = UserRole.Admin,
            Tenant = new Tenant { Name = "Acme Corp" }
        };

        _userRepository.Setup(r => r.GetByUsernameAsync("admin@acme.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("Password123!", "hash")).Returns(true);
        _tokenService.Setup(t => t.GenerateToken(user, "Acme Corp"))
            .Returns(("jwt-token", DateTime.UtcNow.AddHours(8)));

        var sut = CreateSut();
        var result = await sut.LoginAsync(new LoginRequest("admin@acme.com", "Password123!"));

        result.Token.Should().Be("jwt-token");
        result.TenantName.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsUnauthorized()
    {
        var user = new User
        {
            Username = "admin@acme.com",
            PasswordHash = "hash",
            Tenant = new Tenant { Name = "Acme" }
        };

        _userRepository.Setup(r => r.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var sut = CreateSut();
        var act = () => sut.LoginAsync(new LoginRequest("admin@acme.com", "wrong"));

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
