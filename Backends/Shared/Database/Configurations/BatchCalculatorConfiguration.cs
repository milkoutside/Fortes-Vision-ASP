using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.Batches;
using Shared.Domain.Projects;
using System.Text.Json;

namespace Shared.Database.Configurations;

public class BatchCalculatorConfiguration : IEntityTypeConfiguration<BatchCalculator>
{
    public void Configure(EntityTypeBuilder<BatchCalculator> builder)
    {
        builder.ToTable("batch_calculators");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.BatchId)
            .HasColumnName("batch_id");

        builder.HasOne(x => x.Batch)
            .WithMany()
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.ProjectId)
            .HasColumnName("project_id");

        builder.HasOne(x => x.Project)
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.StatusDurations)
            .HasColumnName("status_durations")
            .HasColumnType("json")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<StatusDuration>>(v, (JsonSerializerOptions?)null) ?? new List<StatusDuration>(),
                new ValueComparer<List<StatusDuration>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

        builder.HasIndex(x => x.BatchId);
        builder.HasIndex(x => x.ProjectId);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamp")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();
    }
}

