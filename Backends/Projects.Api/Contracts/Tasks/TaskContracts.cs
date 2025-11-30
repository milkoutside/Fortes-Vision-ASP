using Projects.Api.Contracts.Common;
using Projects.Api.Contracts.Projects;

namespace Projects.Api.Contracts.Tasks;

public record TaskStatusDto(long Id, string Name, string Color, string TextColor);

public record TaskDto(
    long Id,
    long ProjectId,
    long BatchId,
    long ImageId,
    TaskStatusDto Status,
    DateOnly StartDate,
    DateOnly? DueDate,
    DateOnly? EndDate,
    bool Completed,
    IEnumerable<UserDto> Users
);

public class TaskFilterQuery
{
    public long? StatusId { get; set; }
    public bool? Completed { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

public class CreateTaskRequest
{
    public long StatusId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool? Completed { get; set; }
    public UserRoleRequest[]? Users { get; set; }
}

public class UpdateTaskRequest
{
    public long? StatusId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool? Completed { get; set; }
    public UserRoleRequest[]? Users { get; set; }
}

public class BulkCreateTasksRequest
{
    public CreateTaskRequest[] Tasks { get; set; } = Array.Empty<CreateTaskRequest>();
}

public class BulkUpdateTaskRequest : UpdateTaskRequest
{
    public long TaskId { get; set; }
}

public class BulkUpdateTasksRequest
{
    public BulkUpdateTaskRequest[] Tasks { get; set; } = Array.Empty<BulkUpdateTaskRequest>();
}

public class BulkDeleteTasksRequest
{
    public long[] TaskIds { get; set; } = Array.Empty<long>();
}


