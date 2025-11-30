using Microsoft.EntityFrameworkCore;
using Shared.Domain.Batches;
using Shared.Domain.Images;
using Shared.Domain.Projects;
using Shared.Domain.Statuses;
using Shared.Domain.Users;
using TaskEntity = Shared.Domain.Tasks.Task;
using TaskUserEntity = Shared.Domain.Tasks.TaskUser;

namespace Shared.Database;

public class SharedDbContext : DbContext
{
    public SharedDbContext(DbContextOptions<SharedDbContext> options) : base(options)
    {
    }

    public DbSet<Status> Statuses => Set<Status>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<Image> Images => Set<Image>();
    public DbSet<BatchUser> BatchUsers => Set<BatchUser>();
    public DbSet<ProjectUser> ProjectUsers => Set<ProjectUser>();
    public DbSet<ImageUser> ImageUsers => Set<ImageUser>();
    public DbSet<User> Users => Set<User>();
    public DbSet<BatchCalculator> BatchCalculators => Set<BatchCalculator>();
    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();
    public DbSet<TaskUserEntity> TaskUsers => Set<TaskUserEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SharedDbContext).Assembly);
    }
}


