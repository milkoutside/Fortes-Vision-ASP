using System.ComponentModel.DataAnnotations.Schema;
using Shared.Database;
using Shared.Domain.Common;
using Shared.Domain.Users;

namespace Shared.Domain.Projects;

[Table("project_user")]
public class ProjectUser : BaseEntity
{
    [Column("project_id")]
    public long ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    [Column("user_id")]
    public long UserId { get; set; }
    public User User { get; set; } = null!;

    [Column("role")]
    public ParticipantRole Role { get; set; } = ParticipantRole.Freelancer;
}


