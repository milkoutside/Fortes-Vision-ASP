using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shared.Domain.Statuses;

namespace Shared.Application.Statuses;

public interface IStatusService
{
    Task<IReadOnlyList<Status>> GetAllAsync(CancellationToken cancellationToken);
    Task<Status?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<Status> CreateAsync(string name, string color, string? textColor, CancellationToken cancellationToken);
    Task<Status?> UpdateAsync(long id, string? name, string? color, string? textColor, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken);
}


