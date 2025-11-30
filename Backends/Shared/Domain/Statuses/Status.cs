using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Shared.Database;

namespace Shared.Domain.Statuses;

[Table("statuses")]
[Index(nameof(Name), IsUnique = true)]
public class Status : BaseEntity
{
    [Column("name")]
    [MaxLength(255)]
    public string Name { get; set; } = null!;

    [Column("color")]
    [MaxLength(64)]
    public string Color { get; set; } = null!;

    [Column("text_color")]
    [MaxLength(64)]
    public string TextColor { get; set; } = "#000000";
}


