using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.Batches;
using Shared.Domain.Common;
using Shared.Domain.Users;

namespace Shared.Database.Configurations;

public class BatchUserConfiguration : IEntityTypeConfiguration<BatchUser>
{
    public void Configure(EntityTypeBuilder<BatchUser> builder)
    {
        builder.ToTable("batch_user");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.BatchId).HasColumnName("batch_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");

        builder.HasOne(x => x.Batch)
            .WithMany()
            .HasForeignKey(x => x.BatchId)
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

        builder.HasIndex(x => new { x.BatchId, x.UserId }).IsUnique();
    }
}


