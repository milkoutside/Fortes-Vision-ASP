using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Database;

public abstract class BaseEntity
{
    public long Id { get; set; }
    [Column("created_at", TypeName = "timestamp")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    [Column("updated_at", TypeName = "timestamp")]
    public DateTime? UpdatedAtUtc { get; set; }
}


