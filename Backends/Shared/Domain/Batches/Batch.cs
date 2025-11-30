using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Database;
using Shared.Domain.Images;
using Shared.Domain.Projects;

namespace Shared.Domain.Batches;

[Table("batches")]
public class Batch : BaseEntity
{
    [Column("name")]
    [MaxLength(255)]
    public string Name { get; set; } = null!;

    [Column("project_id")]
    public long ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public ICollection<Image> Images { get; set; } = new List<Image>();
}


