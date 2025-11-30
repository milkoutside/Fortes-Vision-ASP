using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projects.Api.Contracts.Batches;
using Projects.Api.Contracts.Common;
using Shared.Database;
using Shared.Domain.Batches;

namespace Projects.Api.Controllers;

[ApiController]
[Route("batch-calculators")]
public class BatchCalculatorsController : ControllerBase
{
    private readonly SharedDbContext _db;

    public BatchCalculatorsController(SharedDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateBatchCalculatorRequest req, CancellationToken ct)
    {
        if (req.StatusDurations == null || req.StatusDurations.Count == 0)
        {
            return BadRequest(ApiResponse.Failure<object>("StatusDurations cannot be empty."));
        }

        var batchExists = await _db.Batches.AnyAsync(b => b.Id == req.BatchId, ct);
        if (!batchExists)
        {
            return BadRequest(ApiResponse.Failure<object>("Batch does not exist."));
        }

        var projectExists = await _db.Projects.AnyAsync(p => p.Id == req.ProjectId, ct);
        if (!projectExists)
        {
            return BadRequest(ApiResponse.Failure<object>("Project does not exist."));
        }

        var statusIds = req.StatusDurations.Select(sd => sd.Status).Distinct().ToArray();
        var statusesExist = await _db.Statuses
            .Where(s => statusIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync(ct);

        var missingStatusIds = statusIds.Except(statusesExist).ToList();
        if (missingStatusIds.Any())
        {
            return BadRequest(ApiResponse.Failure<object>($"Statuses with IDs {string.Join(", ", missingStatusIds)} do not exist."));
        }

        var statusDurations = req.StatusDurations.Select(sd => new StatusDuration
        {
            Status = sd.Status,
            Duration = sd.Duration
        }).ToList();

        var batchCalculator = new BatchCalculator
        {
            BatchId = req.BatchId,
            ProjectId = req.ProjectId,
            StatusDurations = statusDurations
        };

        await _db.BatchCalculators.AddAsync(batchCalculator, ct);
        await _db.SaveChangesAsync(ct);

        return StatusCode(201, ApiResponse.Success<object>(
            new
            {
                batchCalculator.Id,
                batchCalculator.BatchId,
                batchCalculator.ProjectId,
                StatusDurations = batchCalculator.StatusDurations.Select(sd => new { sd.Status, sd.Duration })
            },
            "Batch calculator created successfully."));
    }

    [HttpGet("by-batch/{batchId:long}")]
    public async Task<ActionResult<ApiResponse<object>>> GetByBatch(long batchId, CancellationToken ct)
    {
        var calculator = await _db.BatchCalculators
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.BatchId == batchId, ct);

        if (calculator == null)
        {
            return NotFound(ApiResponse.Failure<object>("Batch calculator not found."));
        }

        return Ok(ApiResponse.Success<object>(
            new
            {
                calculator.Id,
                calculator.BatchId,
                calculator.ProjectId,
                StatusDurations = calculator.StatusDurations.Select(sd => new { sd.Status, sd.Duration })
            },
            "Batch calculator retrieved successfully."));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(long id, [FromBody] CreateBatchCalculatorRequest req, CancellationToken ct)
    {
        var calculator = await _db.BatchCalculators.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (calculator == null)
        {
            return NotFound(ApiResponse.Failure<object>("Batch calculator not found."));
        }

        if (req.StatusDurations == null || req.StatusDurations.Count == 0)
        {
            return BadRequest(ApiResponse.Failure<object>("StatusDurations cannot be empty."));
        }

        var statusIds = req.StatusDurations.Select(sd => sd.Status).Distinct().ToArray();
        var statusesExist = await _db.Statuses
            .Where(s => statusIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync(ct);

        var missingStatusIds = statusIds.Except(statusesExist).ToList();
        if (missingStatusIds.Any())
        {
            return BadRequest(ApiResponse.Failure<object>($"Statuses with IDs {string.Join(", ", missingStatusIds)} do not exist."));
        }

        var statusDurations = req.StatusDurations.Select(sd => new StatusDuration
        {
            Status = sd.Status,
            Duration = sd.Duration
        }).ToList();

        calculator.StatusDurations = statusDurations;
        calculator.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse.Success<object>(
            new
            {
                calculator.Id,
                calculator.BatchId,
                calculator.ProjectId,
                StatusDurations = calculator.StatusDurations.Select(sd => new { sd.Status, sd.Duration })
            },
            "Batch calculator updated successfully."));
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(long id, CancellationToken ct)
    {
        var calculator = await _db.BatchCalculators.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (calculator == null)
        {
            return NotFound(ApiResponse.Failure<object?>("Batch calculator not found."));
        }

        _db.BatchCalculators.Remove(calculator);
        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse.Success<object?>(null, "Batch calculator deleted successfully."));
    }
}

