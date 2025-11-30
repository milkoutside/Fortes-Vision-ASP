using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Database;
using Shared.Domain.Projects;

namespace Shared.Domain.Batches;

[Table("batch_calculators")]
public class BatchCalculator : BaseEntity
{
    [Column("batch_id")]
    public long BatchId { get; set; }
    public Batch Batch { get; set; } = null!;

    [Column("project_id")]
    public long ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    [Column("status_durations", TypeName = "json")]
    public List<StatusDuration> StatusDurations { get; set; } = new();
}

public class StatusDuration
{
    public long Status { get; set; }
    public int Duration { get; set; }

    public override bool Equals(object? obj)
    {
        return obj is StatusDuration other && Status == other.Status && Duration == other.Duration;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Status, Duration);
    }
}

