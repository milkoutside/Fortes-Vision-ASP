using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shared.Database;
using Shared.Domain.Statuses;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Shared.Application.Common;

namespace Shared.Application.Statuses;

public class StatusService : IStatusService
{
    private readonly IStatusRepository _repository;

    public StatusService(IStatusRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<Status>> GetAllAsync(CancellationToken cancellationToken)
        => _repository.GetAllAsync(cancellationToken);

    public Task<Status?> GetByIdAsync(long id, CancellationToken cancellationToken)
        => _repository.GetByIdAsync(id, cancellationToken);

    public async Task<Status> CreateAsync(string name, string color, string? textColor, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required", nameof(name));
        if (string.IsNullOrWhiteSpace(color)) throw new ArgumentException("Color is required", nameof(color));

        // Превентивная проверка уникальности
        if (await _repository.ExistsByNameAsync(name.Trim(), null, cancellationToken))
            throw new ConflictException($"Status with name '{name}' already exists.");

        var entity = new Status
        {
            Name = name.Trim(),
            Color = color.Trim(),
            TextColor = string.IsNullOrWhiteSpace(textColor) ? "#000000" : textColor.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        try
        {
            return await _repository.AddAsync(entity, cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateKey(ex))
        {
            throw new ConflictException($"Status with name '{name}' already exists.");
        }
    }

    public async Task<Status?> UpdateAsync(long id, string? name, string? color, string? textColor, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return null;

        if (!string.IsNullOrWhiteSpace(name)) existing.Name = name.Trim();
        if (!string.IsNullOrWhiteSpace(color)) existing.Color = color.Trim();
        if (!string.IsNullOrWhiteSpace(textColor)) existing.TextColor = textColor.Trim();
        existing.UpdatedAtUtc = DateTime.UtcNow;

        // Превентивная проверка уникальности при изменении имени
        if (!string.IsNullOrWhiteSpace(name))
        {
            if (await _repository.ExistsByNameAsync(existing.Name, existing.Id, cancellationToken))
                throw new ConflictException($"Status with name '{existing.Name}' already exists.");
        }

        try
        {
            var ok = await _repository.UpdateAsync(existing, cancellationToken);
            return ok ? existing : null;
        }
        catch (DbUpdateException ex) when (IsDuplicateKey(ex))
        {
            throw new ConflictException("Status name must be unique.");
        }
    }

    public Task<bool> DeleteAsync(long id, CancellationToken cancellationToken)
        => _repository.DeleteAsync(id, cancellationToken);

    private static bool IsDuplicateKey(DbUpdateException ex)
    {
        // MySQL duplicate key: 1062
        if (ex.InnerException is MySqlException my && my.Number == 1062) return true;
        // Некоторые провайдеры прячут глубже
        if (ex.InnerException?.InnerException is MySqlException my2 && my2.Number == 1062) return true;
        return false;
    }
}


