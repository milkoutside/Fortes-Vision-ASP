using System.ComponentModel.DataAnnotations;

namespace Projects.Api.Contracts.Statuses;

public class UpdateStatusRequest
{
    [MaxLength(255)]
    public string? Name { get; set; }

    [MaxLength(64)]
    public string? Color { get; set; }

    [MaxLength(64)]
    public string? TextColor { get; set; }
}


