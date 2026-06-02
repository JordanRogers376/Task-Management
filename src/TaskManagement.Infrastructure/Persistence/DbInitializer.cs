using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        try
        {
            await context.Database.MigrateAsync();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 1 && environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Database schema is out of sync with migrations. " +
                "Delete src/TaskManagement.Api/taskmanagement.db and restart the API.",
                ex);
        }

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
            Username = "admin@acme.com",
            PasswordHash = passwordHasher.Hash("Password123!"),
            Role = UserRole.Admin
        };

        var userA = new User
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            TenantId = tenantA.Id,
            Username = "user@acme.com",
            PasswordHash = passwordHasher.Hash("Password123!"),
            Role = UserRole.User
        };

        var adminB = new User
        {
            Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            TenantId = tenantB.Id,
            Username = "admin@globex.com",
            PasswordHash = passwordHasher.Hash("Password123!"),
            Role = UserRole.Admin
        };

        context.Tenants.AddRange(tenantA, tenantB);
        context.Users.AddRange(adminA, userA, adminB);
        context.Tasks.AddRange(
            new TaskItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantA.Id,
                AssignedUserId = userA.Id,
                Title = "Review quarterly goals",
                Description = "Prepare notes for the team meeting.",
                IsCompleted = false,
                CreatedDate = DateTime.UtcNow.AddDays(-2)
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantA.Id,
                AssignedUserId = userA.Id,
                Title = "Update documentation",
                IsCompleted = true,
                CreatedDate = DateTime.UtcNow.AddDays(-5),
                CompletedAt = DateTime.UtcNow.AddDays(-1)
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantB.Id,
                AssignedUserId = adminB.Id,
                Title = "Onboard new client",
                IsCompleted = false,
                CreatedDate = DateTime.UtcNow.AddDays(-1)
            });

        await context.SaveChangesAsync();
    }
}
