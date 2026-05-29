using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await context.Database.MigrateAsync();

        if (await context.Tenants.AnyAsync())
            return;

        var tenantA = new Tenant
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Acme Corp",
            CreatedAt = DateTime.UtcNow
        };

        var tenantB = new Tenant
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "Globex Inc",
            CreatedAt = DateTime.UtcNow
        };

        var adminA = new User
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            TenantId = tenantA.Id,
            Email = "admin@acme.com",
            PasswordHash = passwordHasher.Hash("Password123!"),
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        };

        var userA = new User
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            TenantId = tenantA.Id,
            Email = "user@acme.com",
            PasswordHash = passwordHasher.Hash("Password123!"),
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow
        };

        var adminB = new User
        {
            Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            TenantId = tenantB.Id,
            Email = "admin@globex.com",
            PasswordHash = passwordHasher.Hash("Password123!"),
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        };

        context.Tenants.AddRange(tenantA, tenantB);
        context.Users.AddRange(adminA, userA, adminB);
        context.Tasks.AddRange(
            new TaskItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantA.Id,
                CreatedByUserId = adminA.Id,
                Title = "Review quarterly goals",
                Description = "Prepare notes for the team meeting.",
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantA.Id,
                CreatedByUserId = userA.Id,
                Title = "Update documentation",
                IsCompleted = true,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                CompletedAt = DateTime.UtcNow.AddDays(-1)
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantB.Id,
                CreatedByUserId = adminB.Id,
                Title = "Onboard new client",
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });

        await context.SaveChangesAsync();
    }
}
