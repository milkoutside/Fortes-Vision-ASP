using Microsoft.EntityFrameworkCore;
using Shared.Domain.Statuses;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Database.Repositories;

public class StatusRepository : IStatusRepository
{
    private readonly SharedDbContext _dbContext;

    public StatusRepository(SharedDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Status>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Statuses.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<Status?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return await _dbContext.Statuses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Status> AddAsync(Status status, CancellationToken cancellationToken)
    {
        await _dbContext.Statuses.AddAsync(status, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return status;
    }

    public async Task<bool> UpdateAsync(Status status, CancellationToken cancellationToken)
    {
        _dbContext.Statuses.Update(status);
        var affected = await _dbContext.SaveChangesAsync(cancellationToken);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Statuses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return false;
        }
        _dbContext.Statuses.Remove(entity);
        var affected = await _dbContext.SaveChangesAsync(cancellationToken);
        return affected > 0;
    }

    public async Task<bool> ExistsByNameAsync(string name, long? exceptId, CancellationToken cancellationToken)
    {
        var query = _dbContext.Statuses.AsNoTracking().Where(x => x.Name == name);
        if (exceptId.HasValue)
        {
            query = query.Where(x => x.Id != exceptId.Value);
        }
        return await query.AnyAsync(cancellationToken);
    }
}


