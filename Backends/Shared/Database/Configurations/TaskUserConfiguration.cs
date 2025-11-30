using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskUserEntity = Shared.Domain.Tasks.TaskUser;

namespace Shared.Database.Configurations;

public class TaskUserConfiguration : IEntityTypeConfiguration<TaskUserEntity>
{
    public void Configure(EntityTypeBuilder<TaskUserEntity> builder)
    {
        builder.ToTable("task_user");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.TaskId).HasColumnName("task_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasDefaultValue(Shared.Domain.Common.ParticipantRole.Freelancer);

        builder.HasIndex(x => new { x.TaskId, x.UserId }).IsUnique();

        builder.HasOne(x => x.Task)
            .WithMany(t => t.TaskUsers)
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


