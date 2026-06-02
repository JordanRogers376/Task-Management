using AutoMapper;
using TaskManagement.Application.DTOs;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<TaskItem, TaskDto>()
            .ConstructUsing(s => new TaskDto(
                s.Id,
                s.Title,
                s.Description,
                s.IsCompleted,
                s.CreatedDate,
                s.CompletedAt,
                s.AssignedUserId,
                s.AssignedUser != null ? s.AssignedUser.Username : string.Empty));
    }
}
