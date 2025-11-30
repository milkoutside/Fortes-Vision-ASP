using System.ComponentModel.DataAnnotations.Schema;
using Shared.Database;
using Shared.Domain.Common;
using Shared.Domain.Users;

namespace Shared.Domain.Tasks;

[Table("task_user")]
public class TaskUser : BaseEntity
{
    [Column("task_id")]
    public long TaskId { get; set; }
    public Task Task { get; set; } = null!;

    [Column("user_id")]
    public long UserId { get; set; }
    public User User { get; set; } = null!;

    [Column("role")]
    public ParticipantRole Role { get; set; } = ParticipantRole.Freelancer;
}


