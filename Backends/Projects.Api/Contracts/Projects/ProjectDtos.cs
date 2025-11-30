namespace Projects.Api.Contracts.Projects;

public record UserDto(long Id, string Name, string Role);
public record ImageDto(long Id, string Name, IEnumerable<UserDto> Users);
public record BatchDto(long Id, string Name, IEnumerable<ImageDto> Images, IEnumerable<UserDto> Users);
public record ProjectDto(
    long Id,
    string Name,
    bool IsActive,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? ClientName,
    string DeadlineType,
    IEnumerable<BatchDto> Batches,
    IEnumerable<UserDto> Users
);

public class ProjectIndexQuery
{
    public int Page { get; set; } = 1;
    public int PerPage { get; set; } = 10;
    public string? Search { get; set; }
    public long[]? UserIds { get; set; }
}

public class CreateProjectRequest
{
    public string Name { get; set; } = null!;
    public bool? IsActive { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateOnly? StartDate { get; set; }
    public string? ClientName { get; set; }
    public string? DeadlineType { get; set; }
}

public class UpdateProjectRequest
{
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateOnly? StartDate { get; set; }
    public string? ClientName { get; set; }
    public string? DeadlineType { get; set; }
    public long[]? Users { get; set; }
}

public class AttachUsersRequest
{
    public long[] UserIds { get; set; } = Array.Empty<long>();
    public string Role { get; set; } = "freelancer";
}


