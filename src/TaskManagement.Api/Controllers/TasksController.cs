using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly TaskService _taskService;
    private readonly IValidator<CreateTaskRequest> _createValidator;
    private readonly IValidator<UpdateTaskRequest> _updateValidator;

    public TasksController(
        TaskService taskService,
        IValidator<CreateTaskRequest> createValidator,
        IValidator<UpdateTaskRequest> updateValidator)
    {
        _taskService = taskService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> GetTasks(CancellationToken cancellationToken) =>
        Ok(await _taskService.GetTasksForTenantAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDto>> GetTask(Guid id, CancellationToken cancellationToken) =>
        Ok(await _taskService.GetTaskAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Roles = UserRole.Admin)]
    public async Task<ActionResult<TaskDto>> CreateTask(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var task = await _taskService.CreateTaskAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = UserRole.Admin)]
    public async Task<ActionResult<TaskDto>> UpdateTask(
        Guid id,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _taskService.UpdateTaskAsync(id, request, cancellationToken));
    }

    [HttpPatch("{id:guid}/complete")]
    public async Task<ActionResult<TaskDto>> CompleteTask(Guid id, CancellationToken cancellationToken) =>
        Ok(await _taskService.CompleteTaskAsync(id, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = UserRole.Admin)]
    public async Task<IActionResult> DeleteTask(Guid id, CancellationToken cancellationToken)
    {
        await _taskService.DeleteTaskAsync(id, cancellationToken);
        return NoContent();
    }
}
