using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Shared.Database;
using Shared.Domain.Batches;

namespace Shared.Domain.Projects;

public enum DeadlineType
{
    Soft,
    Hard
}

[Table("projects")]
[Index(nameof(Name))]
[Index(nameof(IsActive))]
[Index(nameof(DeadlineType))]
[Index(nameof(StartDate))]
[Index(nameof(EndDate))]
[Index(nameof(ClientName))]
[Index(nameof(IsActive), nameof(DeadlineType))]
public class Project : BaseEntity
{
    [Column("name")]
    [MaxLength(255)]
    public string Name { get; set; } = null!;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("end_date", TypeName = "date")]
    public DateOnly? EndDate { get; set; }

    [Column("start_date", TypeName = "date")]
    public DateOnly? StartDate { get; set; }

    [Column("client_name")]
    [MaxLength(255)]
    public string? ClientName { get; set; }

    [Column("deadline_type")]
    public DeadlineType DeadlineType { get; set; } = DeadlineType.Soft;

    public ICollection<Batch> Batches { get; set; } = new List<Batch>();
}


