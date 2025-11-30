using Projects.Api.Contracts.Common;

namespace Projects.Api.Contracts.Images;

public class CreateImageRequest
{
    public string Name { get; set; } = null!;
    public UserRoleRequest[]? Users { get; set; }
}

public class UpdateImageRequest
{
    public string? Name { get; set; }
    public UserRoleRequest[]? Users { get; set; }
}


