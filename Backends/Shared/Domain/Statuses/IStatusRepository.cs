using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Shared.Domain.Statuses;

public interface IStatusRepository
{
    Task<IReadOnlyList<Status>> GetAllAsync(CancellationToken cancellationToken);
    Task<Status?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<Status> AddAsync(Status status, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(Status status, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string name, long? exceptId, CancellationToken cancellationToken);
}


