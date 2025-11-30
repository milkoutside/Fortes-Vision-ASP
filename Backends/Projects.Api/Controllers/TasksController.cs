using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projects.Api.Contracts.Common;
using Projects.Api.Contracts.Projects;
using Projects.Api.Contracts.Tasks;
using Shared.Database;
using Shared.Domain.Common;
using Shared.Domain.Images;
using Shared.Domain.Statuses;
using Shared.Domain.Users;
using TaskEntity = Shared.Domain.Tasks.Task;
using TaskUserEntity = Shared.Domain.Tasks.TaskUser;

namespace Projects.Api.Controllers;

[ApiController]
[Route("projects/{projectId:long}/batches/{batchId:long}/images/{imageId:long}/tasks")]
public class TasksController : ControllerBase
{
    private readonly SharedDbContext _db;

    public TasksController(SharedDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<TaskDto>>>> Index(long projectId, long batchId, long imageId, [FromQuery] TaskFilterQuery query, CancellationToken ct)
    {
        var image = await LoadImage(projectId, batchId, imageId, ct);
        if (image == null)
        {
            return NotFound(ApiResponse.Failure<IEnumerable<TaskDto>>("Image not found within the provided scope."));
        }

        var tasksQuery = _db.Tasks
            .Include(t => t.Status)
            .Where(t => t.ProjectId == projectId && t.BatchId == batchId && t.ImageId == imageId);

        if (query.StatusId.HasValue) tasksQuery = tasksQuery.Where(t => t.StatusId == query.StatusId.Value);
        if (query.Completed.HasValue) tasksQuery = tasksQuery.Where(t => t.Completed == query.Completed.Value);
        if (query.FromDate.HasValue) tasksQuery = tasksQuery.Where(t => t.StartDate >= query.FromDate.Value);
        if (query.ToDate.HasValue) tasksQuery = tasksQuery.Where(t => t.StartDate <= query.ToDate.Value);

        var tasks = await tasksQuery
            .AsNoTracking()
            .OrderBy(t => t.StartDate)
            .ThenBy(t => t.Id)
            .ToListAsync(ct);

        var dto = await BuildTaskDtos(tasks, ct);
        return Ok(ApiResponse.Success(dto, "Tasks retrieved successfully."));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TaskDto>>> Store(long projectId, long batchId, long imageId, [FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        var image = await LoadImage(projectId, batchId, imageId, ct);
        if (image == null)
        {
            return NotFound(ApiResponse.Failure<TaskDto>("Image not found within the provided scope."));
        }

        if (request == null)
        {
            return BadRequest(ApiResponse.Failure<TaskDto>("Payload is required."));
        }

        if (request.StatusId <= 0)
        {
            return BadRequest(ApiResponse.Failure<TaskDto>("Status identifier must be provided."));
        }

        if (request.StartDate == default)
        {
            return BadRequest(ApiResponse.Failure<TaskDto>("Start date must be provided in YYYY-MM-DD format."));
        }

        var statusExists = await _db.Statuses.AnyAsync(s => s.Id == request.StatusId, ct);
        if (!statusExists)
        {
            return BadRequest(ApiResponse.Failure<TaskDto>("Specified status does not exist."));
        }

        var entity = new TaskEntity
        {
            ProjectId = projectId,
            BatchId = batchId,
            ImageId = imageId,
            StatusId = request.StatusId,
            StartDate = request.StartDate,
            DueDate = request.DueDate,
            EndDate = request.EndDate,
            Completed = request.Completed ?? false
        };

        await _db.Tasks.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);

        await SyncTaskUsers(entity.Id, request.Users, ct);

        var dto = await LoadTaskDto(entity.Id, ct);
        if (dto == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse.Failure<TaskDto>("Task was created but could not be loaded."));
        }

        return StatusCode(StatusCodes.Status201Created, ApiResponse.Success(dto, "Task created successfully."));
    }

    [HttpGet("{taskId:long}")]
    public async Task<ActionResult<ApiResponse<TaskDto>>> Show(long projectId, long batchId, long imageId, long taskId, CancellationToken ct)
    {
        var task = await _db.Tasks
            .Include(t => t.Status)
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.Id == taskId &&
                t.ProjectId == projectId &&
                t.BatchId == batchId &&
                t.ImageId == imageId, ct);

        if (task == null)
        {
            return NotFound(ApiResponse.Failure<TaskDto>("Task not found in the provided scope."));
        }

        var dto = await BuildTaskDtos(new[] { task }, ct);
        return Ok(ApiResponse.Success(dto.First(), "Task retrieved successfully."));
    }

    [HttpPut("{taskId:long}")]
    public async Task<ActionResult<ApiResponse<TaskDto>>> Update(long projectId, long batchId, long imageId, long taskId, [FromBody] UpdateTaskRequest request, CancellationToken ct)
    {
        if (request == null)
        {
            return BadRequest(ApiResponse.Failure<TaskDto>("Payload is required."));
        }

        var task = await _db.Tasks.FirstOrDefaultAsync(t =>
            t.Id == taskId &&
            t.ProjectId == projectId &&
            t.BatchId == batchId &&
            t.ImageId == imageId, ct);

        if (task == null)
        {
            return NotFound(ApiResponse.Failure<TaskDto>("Task not found in the provided scope."));
        }

        if (request.StatusId.HasValue)
        {
            var statusExists = await _db.Statuses.AnyAsync(s => s.Id == request.StatusId.Value, ct);
            if (!statusExists)
            {
                return BadRequest(ApiResponse.Failure<TaskDto>("Specified status does not exist."));
            }
            task.StatusId = request.StatusId.Value;
        }

        if (request.StartDate.HasValue) task.StartDate = request.StartDate.Value;
        if (request.DueDate.HasValue) task.DueDate = request.DueDate.Value;
        if (request.EndDate.HasValue) task.EndDate = request.EndDate.Value;
        if (request.Completed.HasValue) task.Completed = request.Completed.Value;
        task.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        if (request.Users != null)
        {
            await SyncTaskUsers(task.Id, request.Users, ct);
        }

        var dto = await LoadTaskDto(task.Id, ct);
        if (dto == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse.Failure<TaskDto>("Task was updated but could not be loaded."));
        }

        return Ok(ApiResponse.Success(dto, "Task updated successfully."));
    }

    [HttpDelete("{taskId:long}")]
    public async Task<ActionResult<ApiResponse<object?>>> Destroy(long projectId, long batchId, long imageId, long taskId, CancellationToken ct)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t =>
            t.Id == taskId &&
            t.ProjectId == projectId &&
            t.BatchId == batchId &&
            t.ImageId == imageId, ct);

        if (task == null)
        {
            return NotFound(ApiResponse.Failure<object?>("Task not found in the provided scope."));
        }

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Success<object?>(null, "Task deleted successfully."));
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<ApiResponse<IEnumerable<TaskDto>>>> BulkCreate(long projectId, long batchId, long imageId, [FromBody] BulkCreateTasksRequest request, CancellationToken ct)
    {
        var image = await LoadImage(projectId, batchId, imageId, ct);
        if (image == null)
        {
            return NotFound(ApiResponse.Failure<IEnumerable<TaskDto>>("Image not found within the provided scope."));
        }

        if (request?.Tasks == null || request.Tasks.Length == 0)
        {
            return BadRequest(ApiResponse.Failure<IEnumerable<TaskDto>>("At least one task must be provided."));
        }

        if (request.Tasks.Any(t => t.StatusId <= 0 || t.StartDate == default))
        {
            return BadRequest(ApiResponse.Failure<IEnumerable<TaskDto>>("Each task must include status_id and start_date."));
        }

        var statusIds = request.Tasks.Select(t => t.StatusId).Distinct().ToArray();
        var existingStatuses = await _db.Statuses.AsNoTracking().Where(s => statusIds.Contains(s.Id)).Select(s => s.Id).ToListAsync(ct);
        if (existingStatuses.Count != statusIds.Length)
        {
            return BadRequest(ApiResponse.Failure<IEnumerable<TaskDto>>("One or more statuses do not exist."));
        }

        var entities = request.Tasks.Select(t => new TaskEntity
        {
            ProjectId = projectId,
            BatchId = batchId,
            ImageId = imageId,
            StatusId = t.StatusId,
            StartDate = t.StartDate,
            DueDate = t.DueDate,
            EndDate = t.EndDate,
            Completed = t.Completed ?? false
        }).ToList();

        await _db.Tasks.AddRangeAsync(entities, ct);
        await _db.SaveChangesAsync(ct);

        var bulkAssignments = new List<TaskUserEntity>();
        for (var i = 0; i < entities.Count; i++)
        {
            var payload = request.Tasks[i];
            if (payload.Users is { Length: > 0 })
            {
                bulkAssignments.AddRange(payload.Users.Select(u => new TaskUserEntity
                {
                    TaskId = entities[i].Id,
                    UserId = u.UserId,
                    Role = ParseRole(u.Role)
                }));
            }
        }

        if (bulkAssignments.Count > 0)
        {
            await _db.TaskUsers.AddRangeAsync(bulkAssignments, ct);
            await _db.SaveChangesAsync(ct);
        }

        var ids = entities.Select(e => e.Id).ToArray();
        var tasks = await _db.Tasks.Include(t => t.Status).AsNoTracking().Where(t => ids.Contains(t.Id)).ToListAsync(ct);
        var dto = await BuildTaskDtos(tasks, ct);

        return StatusCode(StatusCodes.Status201Created, ApiResponse.Success(dto, "Tasks created successfully."));
    }

    [HttpPut("bulk")]
    public async Task<ActionResult<ApiResponse<IEnumerable<TaskDto>>>> BulkUpdate(long projectId, long batchId, long imageId, [FromBody] BulkUpdateTasksRequest request, CancellationToken ct)
    {
        if (request?.Tasks == null || request.Tasks.Length == 0)
        {
            return BadRequest(ApiResponse.Failure<IEnumerable<TaskDto>>("At least one task must be provided."));
        }

        var ids = request.Tasks.Select(t => t.TaskId).Distinct().ToArray();
        var tasks = await _db.Tasks.Where(t =>
                ids.Contains(t.Id) &&
                t.ProjectId == projectId &&
                t.BatchId == batchId &&
                t.ImageId == imageId)
            .ToListAsync(ct);

        if (tasks.Count != ids.Length)
        {
            return NotFound(ApiResponse.Failure<IEnumerable<TaskDto>>("One or more tasks were not found in the provided scope."));
        }

        var statusIds = request.Tasks.Where(t => t.StatusId.HasValue).Select(t => t.StatusId!.Value).Distinct().ToArray();
        if (statusIds.Length > 0)
        {
            var existingStatuses = await _db.Statuses.AsNoTracking().Where(s => statusIds.Contains(s.Id)).Select(s => s.Id).ToListAsync(ct);
            if (existingStatuses.Count != statusIds.Length)
            {
                return BadRequest(ApiResponse.Failure<IEnumerable<TaskDto>>("One or more statuses do not exist."));
            }
        }

        var taskById = tasks.ToDictionary(t => t.Id, t => t);
        var tasksWithUserUpdates = request.Tasks.Where(t => t.Users != null).Select(t => t.TaskId).Distinct().ToArray();
        var existingAssignments = tasksWithUserUpdates.Length == 0
            ? new List<TaskUserEntity>()
            : await _db.TaskUsers.Where(tu => tasksWithUserUpdates.Contains(tu.TaskId)).ToListAsync(ct);
        var assignmentsByTask = existingAssignments.GroupBy(tu => tu.TaskId).ToDictionary(g => g.Key, g => g.ToList());
        var newAssignments = new List<TaskUserEntity>();

        foreach (var payload in request.Tasks)
        {
            var entity = taskById[payload.TaskId];

            if (payload.StatusId.HasValue) entity.StatusId = payload.StatusId.Value;
            if (payload.StartDate.HasValue) entity.StartDate = payload.StartDate.Value;
            if (payload.DueDate.HasValue) entity.DueDate = payload.DueDate.Value;
            if (payload.EndDate.HasValue) entity.EndDate = payload.EndDate.Value;
            if (payload.Completed.HasValue) entity.Completed = payload.Completed.Value;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            if (payload.Users == null) continue;

            if (assignmentsByTask.TryGetValue(payload.TaskId, out var existing))
            {
                _db.TaskUsers.RemoveRange(existing);
            }

            if (payload.Users.Length > 0)
            {
                newAssignments.AddRange(payload.Users.Select(u => new TaskUserEntity
                {
                    TaskId = payload.TaskId,
                    UserId = u.UserId,
                    Role = ParseRole(u.Role)
                }));
            }
        }

        await _db.SaveChangesAsync(ct);

        if (newAssignments.Count > 0)
        {
            await _db.TaskUsers.AddRangeAsync(newAssignments, ct);
            await _db.SaveChangesAsync(ct);
        }

        var updatedTasks = await _db.Tasks
            .Include(t => t.Status)
            .AsNoTracking()
            .Where(t => ids.Contains(t.Id))
            .ToListAsync(ct);

        var dto = await BuildTaskDtos(updatedTasks, ct);
        return Ok(ApiResponse.Success(dto, "Tasks updated successfully."));
    }

    [HttpDelete("bulk")]
    public async Task<ActionResult<ApiResponse<object?>>> BulkDelete(long projectId, long batchId, long imageId, [FromBody] BulkDeleteTasksRequest request, CancellationToken ct)
    {
        if (request?.TaskIds == null || request.TaskIds.Length == 0)
        {
            return BadRequest(ApiResponse.Failure<object?>("At least one task identifier must be provided."));
        }

        var tasks = await _db.Tasks.Where(t =>
                request.TaskIds.Contains(t.Id) &&
                t.ProjectId == projectId &&
                t.BatchId == batchId &&
                t.ImageId == imageId)
            .ToListAsync(ct);

        // Если задачи не найдены - это нормально, просто возвращаем успех
        // Это может произойти если задачи уже были удалены или не существуют
        if (tasks.Count == 0)
        {
            return Ok(ApiResponse.Success<object?>(null, "0 task(s) deleted (already removed or not found)."));
        }

        _db.Tasks.RemoveRange(tasks);
        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse.Success<object?>(null, $"{tasks.Count} task(s) deleted successfully."));
    }

    private async Task<Image?> LoadImage(long projectId, long batchId, long imageId, CancellationToken ct)
    {
        return await _db.Images
            .Include(i => i.Batch)
            .FirstOrDefaultAsync(i => i.Id == imageId && i.BatchId == batchId && i.Batch.ProjectId == projectId, ct);
    }

    private async Task SyncTaskUsers(long taskId, UserRoleRequest[]? payload, CancellationToken ct)
    {
        var existing = await _db.TaskUsers.Where(x => x.TaskId == taskId).ToListAsync(ct);
        if (existing.Count > 0)
        {
            _db.TaskUsers.RemoveRange(existing);
            await _db.SaveChangesAsync(ct);
        }

        if (payload is not { Length: > 0 }) return;

        var toAdd = payload.Select(u => new TaskUserEntity
        {
            TaskId = taskId,
            UserId = u.UserId,
            Role = ParseRole(u.Role)
        }).ToList();

        await _db.TaskUsers.AddRangeAsync(toAdd, ct);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<TaskDto?> LoadTaskDto(long id, CancellationToken ct)
    {
        var task = await _db.Tasks
            .Include(t => t.Status)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (task == null) return null;

        var dto = await BuildTaskDtos(new[] { task }, ct);
        return dto.FirstOrDefault();
    }

    private async Task<IEnumerable<TaskDto>> BuildTaskDtos(IEnumerable<TaskEntity> items, CancellationToken ct)
    {
        var list = items.ToList();
        if (list.Count == 0) return Array.Empty<TaskDto>();

        var ids = list.Select(t => t.Id).ToArray();
        var taskUsers = await _db.TaskUsers.AsNoTracking().Where(tu => ids.Contains(tu.TaskId)).ToListAsync(ct);
        var userIds = taskUsers.Select(tu => tu.UserId).Distinct().ToArray();
        var users = userIds.Length == 0
            ? new Dictionary<long, User>()
            : await _db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, ct);
        var taskUsersByTask = taskUsers.GroupBy(tu => tu.TaskId).ToDictionary(g => g.Key, g => g.ToList());

        return list.Select(t =>
        {
            var assigned = taskUsersByTask.TryGetValue(t.Id, out var tu)
                ? tu.Select(u => ToUserDto(users, u.UserId, u.Role))
                : Enumerable.Empty<UserDto>();
            return new TaskDto(
                t.Id,
                t.ProjectId,
                t.BatchId,
                t.ImageId,
                ToStatusDto(t.Status),
                t.StartDate,
                t.DueDate,
                t.EndDate,
                t.Completed,
                assigned);
        }).ToList();
    }

    private static TaskStatusDto ToStatusDto(Status status)
    {
        return new TaskStatusDto(status.Id, status.Name, status.Color, status.TextColor);
    }

    private static UserDto ToUserDto(IReadOnlyDictionary<long, User> users, long userId, ParticipantRole role)
    {
        var roleString = role.ToString().ToLowerInvariant();
        if (!users.TryGetValue(userId, out var user))
        {
            return new UserDto(userId, "unknown", roleString);
        }

        return new UserDto(user.Id, user.Name, roleString);
    }

    private static ParticipantRole ParseRole(string? role)
    {
        return role?.ToLower() switch
        {
            "modeller" => ParticipantRole.Modeller,
            "artist" => ParticipantRole.Artist,
            "art_director" => ParticipantRole.Art_Director,
            "project_manager" => ParticipantRole.Project_Manager,
            _ => ParticipantRole.Freelancer
        };
    }
}


