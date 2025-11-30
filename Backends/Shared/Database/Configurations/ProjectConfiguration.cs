using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.Projects;

namespace Shared.Database.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("name");

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(x => x.EndDate)
            .HasColumnName("end_date")
            .HasColumnType("date");

        builder.Property(x => x.StartDate)
            .HasColumnName("start_date")
            .HasColumnType("date");

        builder.Property(x => x.ClientName)
            .HasColumnName("client_name")
            .HasMaxLength(255);

        builder.Property(x => x.DeadlineType)
            .HasColumnName("deadline_type")
            .HasConversion(
                v => v.ToString().ToLower(),
                v => Enum.Parse<DeadlineType>(v, true))
            .HasColumnType("enum('soft','hard')");

        // created_at / updated_at
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        // Индексы описаны атрибутами [Index] на сущности
    }
}


