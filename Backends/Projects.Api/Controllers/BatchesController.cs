using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projects.Api.Contracts.Batches;
using Projects.Api.Contracts.Common;
using Projects.Api.Contracts.Projects;
using Shared.Database;
using Shared.Domain.Batches;
using Shared.Domain.Images;
using Shared.Domain.Users;

namespace Projects.Api.Controllers;

[ApiController]
[Route("batches")]
public class BatchesController : ControllerBase
{
    private readonly SharedDbContext _db;
    public BatchesController(SharedDbContext db) { _db = db; }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<BatchDto>>>> Index(CancellationToken ct)
    {
        var batches = await _db.Batches.AsNoTracking().ToListAsync(ct);
        var batchIds = batches.Select(b => b.Id).ToArray();
        var images = await _db.Images.AsNoTracking().Where(i => batchIds.Contains(i.BatchId)).ToListAsync(ct);
        var imageIds = images.Select(i => i.Id).ToArray();
        var imageUsers = await _db.ImageUsers.AsNoTracking().Where(x => imageIds.Contains(x.ImageId)).ToListAsync(ct);
        var batchUsers = await _db.BatchUsers.AsNoTracking().Where(x => batchIds.Contains(x.BatchId)).ToListAsync(ct);
        var userIds = batchUsers.Select(x => x.UserId)
            .Concat(imageUsers.Select(x => x.UserId))
            .Distinct()
            .ToArray();
        var users = await _db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, ct);
        var imgUsersByImg = imageUsers.GroupBy(iu => iu.ImageId).ToDictionary(g => g.Key, g => g.ToList());
        var imagesByBatch = images.GroupBy(i => i.BatchId).ToDictionary(g => g.Key, g => g.ToList());
        var batchUsersByBatch = batchUsers.GroupBy(bu => bu.BatchId).ToDictionary(g => g.Key, g => g.ToList());

        var data = batches.Select(b =>
        {
            var imgs = imagesByBatch.TryGetValue(b.Id, out var ilist) ? ilist : new List<Image>();
            var imgsDto = imgs.Select(i =>
            {
                var ius = imgUsersByImg.TryGetValue(i.Id, out var lst) ? lst : new List<Shared.Domain.Images.ImageUser>();
                var usDto = ius.Select(u => ToUserDto(users, u.UserId));
                return new ImageDto(i.Id, i.Name, usDto);
            });
            var batchParticipants = batchUsersByBatch.TryGetValue(b.Id, out var buList)
                ? buList.Select(u => ToUserDto(users, u.UserId))
                : Enumerable.Empty<UserDto>();
            return new BatchDto(b.Id, b.Name, imgsDto, batchParticipants);
        });
        return Ok(ApiResponse.Success(data, "Batches retrieved successfully."));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Store([FromBody] CreateBatchRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(ApiResponse.Failure<object>("Batch name is required."));
        var existsProject = await _db.Projects.AnyAsync(p => p.Id == req.ProjectId, ct);
        if (!existsProject) return BadRequest(ApiResponse.Failure<object>("Project does not exist."));
        var b = new Batch { Name = req.Name.Trim(), ProjectId = req.ProjectId };
        await _db.Batches.AddAsync(b, ct);
        await _db.SaveChangesAsync(ct);
        return StatusCode(201, ApiResponse.Success<object>(new { b.Id, b.Name }, "Batch created successfully."));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> Show(long id, CancellationToken ct)
    {
        var b = await _db.Batches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (b == null) return NotFound(ApiResponse.Failure<object>("Batch not found."));
        return Ok(ApiResponse.Success<object>(new { b.Id, b.Name, b.ProjectId }, "Batch retrieved successfully."));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(long id, [FromBody] UpdateBatchRequest req, CancellationToken ct)
    {
        var batch = await _db.Batches.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (batch == null) return NotFound(ApiResponse.Failure<object>("Batch not found."));
        if (req.Name != null)
        {
            if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(ApiResponse.Failure<object>("Batch name cannot be empty."));
            batch.Name = req.Name.Trim();
        }
        if (req.ProjectId.HasValue)
        {
            var existsProject = await _db.Projects.AnyAsync(p => p.Id == req.ProjectId, ct);
            if (!existsProject) return BadRequest(ApiResponse.Failure<object>("Project does not exist."));
            batch.ProjectId = req.ProjectId.Value;
        }
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Success<object>(new { batch.Id, batch.Name, batch.ProjectId }, "Batch updated successfully."));
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<object?>>> Destroy(long id, CancellationToken ct)
    {
        var batch = await _db.Batches.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (batch == null) return NotFound(ApiResponse.Failure<object?>("Batch not found."));
        _db.Batches.Remove(batch);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Success<object?>(null, "Batch deleted successfully."));
    }

    [HttpGet("by-project/{projectId:long}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> ByProject(long projectId, CancellationToken ct)
    {
        var batches = await _db.Batches.AsNoTracking().Where(b => b.ProjectId == projectId).ToListAsync(ct);
        var data = batches.Select(b => new { b.Id, b.Name, b.ProjectId });
        return Ok(ApiResponse.Success<IEnumerable<object>>(data, "Batches retrieved successfully."));
    }

    private static UserDto ToUserDto(IReadOnlyDictionary<long, User> users, long userId)
    {
        return users.TryGetValue(userId, out var user)
            ? new UserDto(user.Id, user.Name, user.Role)
            : new UserDto(userId, "unknown", "unknown");
    }
}


