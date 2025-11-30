using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskEntity = Shared.Domain.Tasks.Task;

namespace Shared.Database.Configurations;

public class TaskConfiguration : IEntityTypeConfiguration<TaskEntity>
{
    public void Configure(EntityTypeBuilder<TaskEntity> builder)
    {
        builder.ToTable("tasks");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();

        builder.Property(t => t.StartDate)
            .HasColumnName("start_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(t => t.DueDate)
            .HasColumnName("due_date")
            .HasColumnType("date");

        builder.Property(t => t.EndDate)
            .HasColumnName("end_date")
            .HasColumnType("date");

        builder.Property(t => t.Completed)
            .HasColumnName("completed")
            .HasDefaultValue(false);

        builder.Property(t => t.ProjectId).HasColumnName("project_id");
        builder.Property(t => t.BatchId).HasColumnName("batch_id");
        builder.Property(t => t.ImageId).HasColumnName("image_id");
        builder.Property(t => t.StatusId).HasColumnName("status_id");

        builder.HasOne(t => t.Project)
            .WithMany()
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Batch)
            .WithMany()
            .HasForeignKey(t => t.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Image)
            .WithMany()
            .HasForeignKey(t => t.ImageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Status)
            .WithMany()
            .HasForeignKey(t => t.StatusId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


