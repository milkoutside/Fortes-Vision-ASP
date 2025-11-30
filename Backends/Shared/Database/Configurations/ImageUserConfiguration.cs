using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.Common;
using Shared.Domain.Images;

namespace Shared.Database.Configurations;

public class ImageUserConfiguration : IEntityTypeConfiguration<ImageUser>
{
    public void Configure(EntityTypeBuilder<ImageUser> builder)
    {
        builder.ToTable("image_user");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.ImageId).HasColumnName("image_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");

        builder.HasOne(x => x.Image)
            .WithMany()
            .HasForeignKey(x => x.ImageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Role)
            .HasConversion(
                v => v.ToString().ToLower(),
                v => Enum.Parse<ParticipantRole>(v, true))
            .HasColumnType("enum('modeller','freelancer','artist','art_director','project_manager')");

        builder.HasIndex(x => new { x.ImageId, x.UserId }).IsUnique();
    }
}


