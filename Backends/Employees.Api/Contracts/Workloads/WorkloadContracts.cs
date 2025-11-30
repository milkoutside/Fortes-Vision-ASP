namespace Employees.Api.Contracts.Workloads;

public record WorkloadResponse(
    DateOnly From,
    DateOnly To,
    IReadOnlyCollection<UserWorkloadDto> Users
);

public record UserWorkloadDto(
    long UserId,
    string UserName,
    string UserRole,
    IReadOnlyCollection<WorkloadSegmentDto> SummarySegments,
    IReadOnlyCollection<UserProjectWorkloadDto> Projects
);

public record UserProjectWorkloadDto(
    long ProjectId,
    string ProjectName,
    string? ClientName,
    bool IsActive,
    IReadOnlyCollection<WorkloadSegmentDto> Segments,
    IReadOnlyCollection<UserTaskWorkloadDto> Tasks
);

public record WorkloadSegmentDto(
    long ProjectId,
    string ProjectName,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Tooltip
);

public record UserTaskWorkloadDto(
    long TaskId,
    long ProjectId,
    string ProjectName,
    long BatchId,
    string BatchName,
    long ImageId,
    string ImageName,
    TaskStatusDto Status,
    DateOnly StartDate,
    DateOnly EndDate,
    bool Completed
);

public record TaskStatusDto(long Id, string Name, string Color, string TextColor);

public class WorkloadQuery
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public long[]? UserIds { get; set; }
    public long[]? ProjectIds { get; set; }
    public bool? OnlyActiveProjects { get; set; }
    public string? Search { get; set; }
}

