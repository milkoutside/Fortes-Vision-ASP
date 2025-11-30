namespace Projects.Api.Contracts.Common;

public class UserRoleRequest
{
    public long UserId { get; set; }
    public string Role { get; set; } = "artist";
}


