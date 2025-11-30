using System.ComponentModel.DataAnnotations;

namespace Projects.Api.Contracts.Statuses;

public class CreateStatusRequest
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = null!;

    [Required]
    [MaxLength(64)]
    public string Color { get; set; } = null!;

    [MaxLength(64)]
    public string? TextColor { get; set; }
}


