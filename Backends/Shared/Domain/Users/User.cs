using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Database;

namespace Shared.Domain.Users;

[Table("users")]
public class User : BaseEntity
{
    [Column("name")]
    [MaxLength(255)]
    public string Name { get; set; } = null!;

    // В Laravel поле role строковое — оставляем string
    [Column("role")]
    [MaxLength(64)]
    public string Role { get; set; } = null!;
}


