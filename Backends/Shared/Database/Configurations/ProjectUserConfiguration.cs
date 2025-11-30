using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.Common;
using Shared.Domain.Projects;
using Shared.Domain.Users;

namespace Shared.Database.Configurations;

public class ProjectUserConfiguration : IEntityTypeConfiguration<ProjectUser>
{
    public void Configure(EntityTypeBuilder<ProjectUser> builder)
    {
        builder.ToTable("project_user");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.ProjectId).HasColumnName("project_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");

        builder.HasOne(x => x.Project)
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
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

        builder.HasIndex(x => new { x.ProjectId, x.UserId }).IsUnique();
    }
}


