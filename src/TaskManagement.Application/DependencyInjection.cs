using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.Mapping;
using TaskManagement.Application.Services;
using TaskManagement.Application.Validators;

namespace TaskManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
        services.AddAutoMapper(typeof(MappingProfile).Assembly);
        services.AddScoped<AuthService>();
        services.AddScoped<TaskService>();
        services.AddScoped<DashboardService>();
        return services;
    }
}
