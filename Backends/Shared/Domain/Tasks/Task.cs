using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Shared.Database;
using Shared.Domain.Batches;
using Shared.Domain.Images;
using Shared.Domain.Projects;
using Shared.Domain.Statuses;

namespace Shared.Domain.Tasks;

[Table("tasks")]
[Index(nameof(ProjectId))]
[Index(nameof(BatchId))]
[Index(nameof(ImageId))]
[Index(nameof(StatusId))]
[Index(nameof(StartDate))]
[Index(nameof(EndDate))]
[Index(nameof(Completed))]
public class Task : BaseEntity
{
    [Column("project_id")]
    public long ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    [Column("batch_id")]
    public long BatchId { get; set; }
    public Batch Batch { get; set; } = null!;

    [Column("image_id")]
    public long ImageId { get; set; }
    public Image Image { get; set; } = null!;

    [Column("status_id")]
    public long StatusId { get; set; }
    public Status Status { get; set; } = null!;

    [Column("start_date", TypeName = "date")]
    public DateOnly StartDate { get; set; }

    [Column("due_date", TypeName = "date")]
    public DateOnly? DueDate { get; set; }

    [Column("end_date", TypeName = "date")]
    public DateOnly? EndDate { get; set; }

    [Column("completed")]
    public bool Completed { get; set; } = false;

    public ICollection<TaskUser> TaskUsers { get; set; } = new List<TaskUser>();
}


