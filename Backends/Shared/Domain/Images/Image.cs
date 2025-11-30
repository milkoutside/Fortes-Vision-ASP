using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Database;
using Shared.Domain.Batches;

namespace Shared.Domain.Images;

[Table("images")]
public class Image : BaseEntity
{
    [Column("name")]
    [MaxLength(255)]
    public string Name { get; set; } = null!;

    [Column("batch_id")]
    public long BatchId { get; set; }
    public Batch Batch { get; set; } = null!;
}


