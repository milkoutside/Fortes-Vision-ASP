using System.Globalization;
using Employees.Api.Contracts.Common;
using Employees.Api.Contracts.Workloads;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Database;
using TaskUserEntity = Shared.Domain.Tasks.TaskUser;

namespace Employees.Api.Controllers;

[ApiController]
[Route("workloads")]
public class WorkloadsController : ControllerBase
{
    private readonly SharedDbContext _db;
    private static readonly CultureInfo RuCulture = CultureInfo.GetCultureInfo("ru-RU");

    public WorkloadsController(SharedDbContext db)
    {
        _db = db;
    }

    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<WorkloadResponse>>> GetUserWorkloads([FromQuery] WorkloadQuery query, CancellationToken ct)
    {
        var (fromDate, toDate) = ResolveRange(query);
        if (toDate < fromDate)
        {
            return BadRequest(ApiResponse.Failure<WorkloadResponse>("Parameter 'to' must be greater than or equal to 'from'."));
        }

        var userIds = NormalizeIds(query.UserIds);
        var projectIds = NormalizeIds(query.ProjectIds);

        // Получаем всех пользователей (для отображения даже без активности)
        var usersQuery = _db.Users.AsNoTracking();
        if (userIds is { Length: > 0 })
        {
            usersQuery = usersQuery.Where(u => userIds.Contains(u.Id));
        }
        
        // Применяем поиск к пользователям
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchTerm = $"%{query.Search.Trim()}%";
            usersQuery = usersQuery.Where(u => EF.Functions.Like(u.Name, searchTerm));
        }
        
        var allUsers = await usersQuery.OrderBy(u => u.Name).ToListAsync(ct);

        var assignmentsQuery = _db.TaskUsers
            .AsNoTracking()
            .Include(tu => tu.User)
            .Include(tu => tu.Task)
                .ThenInclude(t => t.Project)
            .Include(tu => tu.Task)
                .ThenInclude(t => t.Batch)
            .Include(tu => tu.Task)
                .ThenInclude(t => t.Image)
            .Include(tu => tu.Task)
                .ThenInclude(t => t.Status)
            .Where(tu => tu.Task.StartDate <= toDate)
            .Where(tu => (tu.Task.EndDate ?? tu.Task.DueDate ?? tu.Task.StartDate) >= fromDate);

        if (userIds is { Length: > 0 })
        {
            assignmentsQuery = assignmentsQuery.Where(tu => userIds.Contains(tu.UserId));
        }

        if (projectIds is { Length: > 0 })
        {
            assignmentsQuery = assignmentsQuery.Where(tu => projectIds.Contains(tu.Task.ProjectId));
        }

        if (query.OnlyActiveProjects == true)
        {
            assignmentsQuery = assignmentsQuery.Where(tu => tu.Task.Project.IsActive);
        }

        // Применяем поиск к заданиям (поиск по имени пользователя, проекта или изображения)
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchTerm = $"%{query.Search.Trim()}%";
            assignmentsQuery = assignmentsQuery.Where(tu => 
                EF.Functions.Like(tu.User.Name, searchTerm) ||
                EF.Functions.Like(tu.Task.Project.Name, searchTerm) ||
                (tu.Task.Image.Name != null && EF.Functions.Like(tu.Task.Image.Name, searchTerm)) ||
                (tu.Task.Project.ClientName != null && EF.Functions.Like(tu.Task.Project.ClientName, searchTerm))
            );
        }

        var assignments = await assignmentsQuery
            .OrderBy(tu => tu.User.Name)
            .ThenBy(tu => tu.Task.Project.Name)
            .ThenBy(tu => tu.Task.StartDate)
            .ToListAsync(ct);

        var response = BuildResponse(assignments, fromDate, toDate, allUsers);
        return Ok(ApiResponse.Success(response, "User workloads loaded successfully."));
    }

    private static (DateOnly From, DateOnly To) ResolveRange(WorkloadQuery query)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentMonthStart = new DateOnly(today.Year, today.Month, 1);
        var from = query.From ?? currentMonthStart.AddMonths(-1);
        var to = query.To ?? currentMonthStart.AddMonths(2).AddDays(-1);
        return (from, to);
    }

    private static long[]? NormalizeIds(long[]? ids)
    {
        return ids is not { Length: > 0 }
            ? null
            : ids.Where(id => id > 0).Distinct().ToArray();
    }

    private static WorkloadResponse BuildResponse(IEnumerable<TaskUserEntity> assignments, DateOnly from, DateOnly to, IEnumerable<Shared.Domain.Users.User> allUsers)
    {
        var spans = assignments
            .Select(a => ToAssignmentSpan(a, from, to))
            .Where(span => span != null)
            .Select(span => span!)
            .ToList();

        var usersWithActivity = spans
            .GroupBy(span => span.UserId)
            .OrderBy(group => group.First().UserName, StringComparer.CurrentCultureIgnoreCase)
            .Select(group => BuildUserDto(group))
            .ToList();

        // Создаем список пользователей без активности
        var activeUserIds = usersWithActivity.Select(u => u.UserId).ToHashSet();
        var usersWithoutActivity = allUsers
            .Where(u => !activeUserIds.Contains(u.Id))
            .Select(u => new UserWorkloadDto(
                u.Id,
                u.Name,
                u.Role,
                Array.Empty<WorkloadSegmentDto>(),
                Array.Empty<UserProjectWorkloadDto>()
            ))
            .ToList();

        // Объединяем оба списка
        var allUserDtos = usersWithActivity
            .Concat(usersWithoutActivity)
            .OrderBy(u => u.UserName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new WorkloadResponse(from, to, allUserDtos);
    }

    private static AssignmentSpan? ToAssignmentSpan(TaskUserEntity entity, DateOnly from, DateOnly to)
    {
        var task = entity.Task;
        var user = entity.User;
        if (task == null || user == null)
        {
            return null;
        }

        var project = task.Project;
        var batch = task.Batch;
        var image = task.Image;
        var status = task.Status;
        if (project == null || batch == null || image == null || status == null)
        {
            return null;
        }

        var effectiveEnd = task.EndDate ?? task.DueDate ?? task.StartDate;
        if (effectiveEnd < task.StartDate)
        {
            effectiveEnd = task.StartDate;
        }

        var clampedStart = MaxDate(task.StartDate, from);
        var clampedEnd = MinDate(effectiveEnd, to);
        if (clampedEnd < clampedStart)
        {
            return null;
        }

        return new AssignmentSpan(
            user.Id,
            user.Name,
            user.Role,
            project.Id,
            project.Name,
            project.ClientName,
            project.IsActive,
            task.Id,
            new TaskStatusDto(status.Id, status.Name, status.Color, status.TextColor),
            clampedStart,
            clampedEnd,
            task.Completed,
            batch.Id,
            batch.Name,
            image.Id,
            image.Name
        );
    }

    private static UserWorkloadDto BuildUserDto(IGrouping<long, AssignmentSpan> userGroup)
    {
        var sample = userGroup.First();
        var projects = userGroup
            .GroupBy(span => span.ProjectId)
            .OrderBy(group => group.First().ProjectName, StringComparer.CurrentCultureIgnoreCase)
            .Select(group => BuildProjectDto(group))
            .ToList();

        var summary = projects
            .SelectMany(p => p.Segments)
            .OrderBy(segment => segment.StartDate)
            .ToList();

        return new UserWorkloadDto(
            sample.UserId,
            sample.UserName,
            sample.UserRole,
            summary,
            projects
        );
    }

    private static UserProjectWorkloadDto BuildProjectDto(IGrouping<long, AssignmentSpan> projectGroup)
    {
        var sample = projectGroup.First();
        var segments = BuildSegments(sample.ProjectId, sample.ProjectName, projectGroup);
        var tasks = projectGroup
            .OrderBy(span => span.StartDate)
            .ThenBy(span => span.TaskId)
            .Select(span => new UserTaskWorkloadDto(
                span.TaskId,
                span.ProjectId,
                span.ProjectName,
                span.BatchId,
                span.BatchName,
                span.ImageId,
                span.ImageName,
                span.Status,
                span.StartDate,
                span.EndDate,
                span.Completed
            ))
            .ToList();

        return new UserProjectWorkloadDto(
            sample.ProjectId,
            sample.ProjectName,
            sample.ClientName,
            sample.ProjectIsActive,
            segments,
            tasks
        );
    }

    private static IReadOnlyCollection<WorkloadSegmentDto> BuildSegments(long projectId, string projectName, IEnumerable<AssignmentSpan> spans)
    {
        var ordered = spans
            .Select(span => (Start: span.StartDate, End: span.EndDate))
            .OrderBy(range => range.Start)
            .ToList();

        if (ordered.Count == 0)
        {
            return Array.Empty<WorkloadSegmentDto>();
        }

        var result = new List<WorkloadSegmentDto>();
        var currentStart = ordered[0].Start;
        var currentEnd = ordered[0].End;

        for (var i = 1; i < ordered.Count; i++)
        {
            var range = ordered[i];
            if (range.Start <= currentEnd.AddDays(1))
            {
                if (range.End > currentEnd)
                {
                    currentEnd = range.End;
                }
                continue;
            }

            result.Add(CreateSegment(projectId, projectName, currentStart, currentEnd));
            currentStart = range.Start;
            currentEnd = range.End;
        }

        result.Add(CreateSegment(projectId, projectName, currentStart, currentEnd));
        return result;
    }

    private static WorkloadSegmentDto CreateSegment(long projectId, string projectName, DateOnly start, DateOnly end)
    {
        var tooltip = $"{projectName}: {FormatDate(start)} — {FormatDate(end)}";
        return new WorkloadSegmentDto(projectId, projectName, start, end, tooltip);
    }

    private static string FormatDate(DateOnly date) => date.ToString("dd MMM", RuCulture);

    private static DateOnly MaxDate(DateOnly left, DateOnly right) => left >= right ? left : right;

    private static DateOnly MinDate(DateOnly left, DateOnly right) => left <= right ? left : right;

    private sealed record AssignmentSpan(
        long UserId,
        string UserName,
        string UserRole,
        long ProjectId,
        string ProjectName,
        string? ClientName,
        bool ProjectIsActive,
        long TaskId,
        TaskStatusDto Status,
        DateOnly StartDate,
        DateOnly EndDate,
        bool Completed,
        long BatchId,
        string BatchName,
        long ImageId,
        string ImageName
    );
}

