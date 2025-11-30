using System.ComponentModel.DataAnnotations.Schema;
using Shared.Database;
using Shared.Domain.Common;
using Shared.Domain.Users;

namespace Shared.Domain.Images;

[Table("image_user")]
public class ImageUser : BaseEntity
{
    [Column("image_id")]
    public long ImageId { get; set; }
    public Image Image { get; set; } = null!;

    [Column("user_id")]
    public long UserId { get; set; }
    public User User { get; set; } = null!;

    [Column("role")]
    public ParticipantRole Role { get; set; } = ParticipantRole.Freelancer;
}


