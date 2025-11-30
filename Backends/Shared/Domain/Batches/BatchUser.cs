using System.ComponentModel.DataAnnotations.Schema;
using Shared.Database;
using Shared.Domain.Common;
using Shared.Domain.Users;

namespace Shared.Domain.Batches;

[Table("batch_user")]
public class BatchUser : BaseEntity
{
    [Column("batch_id")]
    public long BatchId { get; set; }
    public Batch Batch { get; set; } = null!;

    [Column("user_id")]
    public long UserId { get; set; }
    public User User { get; set; } = null!;

    [Column("role")]
    public ParticipantRole Role { get; set; } = ParticipantRole.Freelancer;
}


