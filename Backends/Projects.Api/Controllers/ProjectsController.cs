using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projects.Api.Contracts.Common;
using Projects.Api.Contracts.Projects;
using Shared.Database;
using Shared.Domain.Common;
using Shared.Domain.Images;
using Shared.Domain.Projects;
using Shared.Domain.Users;

namespace Projects.Api.Controllers;

[ApiController]
[Route("projects")]
public class ProjectsController : ControllerBase
{
    private readonly SharedDbContext _db;

    public ProjectsController(SharedDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProjectDto>>> Index([FromQuery] ProjectIndexQuery query, CancellationToken ct)
    {
        var projectQuery = _db.Projects.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            var like = $"%{term}%";
            projectQuery = projectQuery.Where(p =>
                EF.Functions.Like(p.Name, like) ||
                (p.ClientName != null && EF.Functions.Like(p.ClientName, like)));
        }

        if (query.UserIds is { Length: > 0 })
        {
            var ids = query.UserIds.Distinct().ToArray();
            projectQuery = projectQuery.Where(p =>
                _db.ProjectUsers.Any(pu => pu.ProjectId == p.Id && ids.Contains(pu.UserId)));
        }

        var perPage = Math.Max(1, query.PerPage);
        var currentPage = Math.Max(1, query.Page);
        var total = await projectQuery.CountAsync(ct);
        var skip = (currentPage - 1) * perPage;

        var items = await projectQuery
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip(skip)
            .Take(perPage)
            .AsNoTracking()
            .ToListAsync(ct);

        var projectIds = items.Select(p => p.Id).ToArray();
        var batches = await _db.Batches.AsNoTracking().Where(b => projectIds.Contains(b.ProjectId)).ToListAsync(ct);
        var batchIds = batches.Select(b => b.Id).ToArray();
        var images = await _db.Images.AsNoTracking().Where(i => batchIds.Contains(i.BatchId)).ToListAsync(ct);
        var imageIds = images.Select(i => i.Id).ToArray();

        var projectUsers = await _db.ProjectUsers.AsNoTracking().Where(x => projectIds.Contains(x.ProjectId)).ToListAsync(ct);
        var batchUsers = await _db.BatchUsers.AsNoTracking().Where(x => batchIds.Contains(x.BatchId)).ToListAsync(ct);
        var imageUsers = await _db.ImageUsers.AsNoTracking().Where(x => imageIds.Contains(x.ImageId)).ToListAsync(ct);
        var userIds = projectUsers.Select(x => x.UserId)
            .Concat(batchUsers.Select(x => x.UserId))
            .Concat(imageUsers.Select(x => x.UserId))
            .Distinct()
            .ToArray();
        var users = await _db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, ct);

        var batchByProject = batches.GroupBy(b => b.ProjectId).ToDictionary(g => g.Key, g => g.ToList());
        var imagesByBatch = images.GroupBy(i => i.BatchId).ToDictionary(g => g.Key, g => g.ToList());
        var projUsersByProj = projectUsers.GroupBy(pu => pu.ProjectId).ToDictionary(g => g.Key, g => g.ToList());
        var batchUsersByBatch = batchUsers.GroupBy(bu => bu.BatchId).ToDictionary(g => g.Key, g => g.ToList());
        var imgUsersByImg = imageUsers.GroupBy(iu => iu.ImageId).ToDictionary(g => g.Key, g => g.ToList());

        var data = items.Select(p =>
        {
            var pUsers = projUsersByProj.TryGetValue(p.Id, out var pus) ? pus : new List<ProjectUser>();
            var pUsersDto = pUsers.Select(u => ToUserDto(users, u.UserId));

            var projBatches = batchByProject.TryGetValue(p.Id, out var blist) ? blist : new List<Shared.Domain.Batches.Batch>();
            var batchesDto = projBatches.Select(b =>
            {
                var imgs = imagesByBatch.TryGetValue(b.Id, out var ilist) ? ilist : new List<Image>();
                var bu = batchUsersByBatch.TryGetValue(b.Id, out var bulist) ? bulist : new List<Shared.Domain.Batches.BatchUser>();
                var buDto = bu.Select(u => ToUserDto(users, u.UserId));
                var imgsDto = imgs.Select(i =>
                {
                    var ius = imgUsersByImg.TryGetValue(i.Id, out var iulist) ? iulist : new List<ImageUser>();
                    var iusDto = ius.Select(u => ToUserDto(users, u.UserId));
                    return new ImageDto(i.Id, i.Name, iusDto);
                });
                return new BatchDto(b.Id, b.Name, imgsDto, buDto);
            });

            return new ProjectDto(
                p.Id,
                p.Name,
                p.IsActive,
                p.StartDate,
                p.EndDate,
                p.ClientName,
                p.DeadlineType.ToString().ToLowerInvariant(),
                batchesDto,
                pUsersDto);
        }).ToList();

        var lastPage = (int)Math.Ceiling(total / (double)perPage);
        var pagination = new Pagination(currentPage, perPage, total, lastPage);
        return Ok(new PagedResponse<ProjectDto>(true, data, pagination, "Projects retrieved successfully."));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> Store([FromBody] CreateProjectRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            return BadRequest(ApiResponse.Failure<ProjectDto>("Project name is required."));
        }

        var entity = new Project
        {
            Name = req.Name.Trim(),
            IsActive = req.IsActive ?? true,
            EndDate = req.EndDate,
            StartDate = req.StartDate,
            ClientName = string.IsNullOrWhiteSpace(req.ClientName) ? null : req.ClientName.Trim(),
            DeadlineType = string.Equals(req.DeadlineType, "hard", StringComparison.OrdinalIgnoreCase) ? DeadlineType.Hard : DeadlineType.Soft
        };

        await _db.Projects.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);

        var dto = await LoadProjectDto(entity.Id, ct);
        if (dto == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse.Failure<ProjectDto>("Project was created but could not be loaded."));
        }

        return CreatedAtAction(nameof(Show), new { id = entity.Id }, ApiResponse.Success(dto, "Project has been created successfully."));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> Show(long id, CancellationToken ct)
    {
        var dto = await LoadProjectDto(id, ct);
        if (dto == null) return NotFound(ApiResponse.Failure<ProjectDto>("Project not found."));
        return Ok(ApiResponse.Success(dto, "Project retrieved successfully."));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> Update(long id, [FromBody] UpdateProjectRequest req, CancellationToken ct)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (project == null) return NotFound(ApiResponse.Failure<ProjectDto>("Project not found."));

        if (req.Name != null)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
            {
                return BadRequest(ApiResponse.Failure<ProjectDto>("Project name cannot be empty."));
            }
            project.Name = req.Name.Trim();
        }
        if (req.IsActive.HasValue) project.IsActive = req.IsActive.Value;
        if (req.EndDate.HasValue) project.EndDate = req.EndDate.Value;
        if (req.StartDate.HasValue) project.StartDate = req.StartDate.Value;
        if (req.ClientName != null) project.ClientName = string.IsNullOrWhiteSpace(req.ClientName) ? null : req.ClientName.Trim();
        if (req.DeadlineType != null)
        {
            project.DeadlineType = string.Equals(req.DeadlineType, "hard", StringComparison.OrdinalIgnoreCase)
                ? DeadlineType.Hard
                : DeadlineType.Soft;
        }
        project.UpdatedAtUtc = DateTime.UtcNow;

        if (req.Users != null)
        {
            var selected = req.Users.Distinct().ToArray();
            var existing = await _db.ProjectUsers.Where(x => x.ProjectId == project.Id && selected.Contains(x.UserId)).ToListAsync(ct);

            var existingByUser = existing.ToDictionary(x => x.UserId, x => x);
            var toKeep = new List<ProjectUser>();
            foreach (var uid in selected)
            {
                if (existingByUser.TryGetValue(uid, out var ex))
                {
                    toKeep.Add(ex);
                }
                else
                {
                    toKeep.Add(new ProjectUser { ProjectId = project.Id, UserId = uid, Role = ParticipantRole.Freelancer });
                }
            }

            var current = await _db.ProjectUsers.Where(x => x.ProjectId == project.Id).ToListAsync(ct);
            _db.ProjectUsers.RemoveRange(current);
            if (toKeep.Count > 0)
            {
                await _db.ProjectUsers.AddRangeAsync(toKeep, ct);
            }
        }

        await _db.SaveChangesAsync(ct);
        var dto = await LoadProjectDto(project.Id, ct);
        if (dto == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse.Failure<ProjectDto>("Project was updated but could not be loaded."));
        }

        return Ok(ApiResponse.Success(dto, "Project updated successfully."));
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<object?>>> Destroy(long id, CancellationToken ct)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (project == null) return NotFound(ApiResponse.Failure<object?>("Project not found."));
        _db.Projects.Remove(project);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Success<object?>(null, "Project deleted successfully."));
    }

    [HttpPost("{id:long}/attach-users")]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> AttachUsers(long id, [FromBody] AttachUsersRequest req, CancellationToken ct)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (project == null) return NotFound(ApiResponse.Failure<ProjectDto>("Project not found."));

        if (req.UserIds == null || req.UserIds.Length == 0)
        {
            return BadRequest(ApiResponse.Failure<ProjectDto>("At least one user identifier must be provided."));
        }

        var role = ParseRole(req.Role);
        var toAdd = req.UserIds.Distinct().Select(uid => new ProjectUser { ProjectId = id, UserId = uid, Role = role });
        await _db.ProjectUsers.AddRangeAsync(toAdd, ct);
        await _db.SaveChangesAsync(ct);

        var dto = await LoadProjectDto(id, ct);
        if (dto == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse.Failure<ProjectDto>("Users were attached but the project could not be loaded."));
        }

        return Ok(ApiResponse.Success(dto, "Users attached successfully."));
    }

    [HttpGet("{id:long}/batches")]
    public async Task<ActionResult<ApiResponse<IEnumerable<BatchDto>>>> GetBatches(long id, CancellationToken ct)
    {
        var batches = await _db.Batches.Where(b => b.ProjectId == id).AsNoTracking().ToListAsync(ct);
        var batchIds = batches.Select(b => b.Id).ToArray();
        var images = await _db.Images.AsNoTracking().Where(i => batchIds.Contains(i.BatchId)).ToListAsync(ct);
        var imageIds = images.Select(i => i.Id).ToArray();

        var batchUsers = await _db.BatchUsers.AsNoTracking().Where(x => batchIds.Contains(x.BatchId)).ToListAsync(ct);
        var imageUsers = await _db.ImageUsers.AsNoTracking().Where(x => imageIds.Contains(x.ImageId)).ToListAsync(ct);
        var userIds = batchUsers.Select(x => x.UserId)
            .Concat(imageUsers.Select(x => x.UserId))
            .Distinct()
            .ToArray();
        var users = await _db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, ct);

        var batchUsersByBatch = batchUsers.GroupBy(bu => bu.BatchId).ToDictionary(g => g.Key, g => g.ToList());
        var imgUsersByImg = imageUsers.GroupBy(iu => iu.ImageId).ToDictionary(g => g.Key, g => g.ToList());
        var imagesByBatch = images.GroupBy(i => i.BatchId).ToDictionary(g => g.Key, g => g.ToList());

        var data = batches.Select(b =>
        {
            var imgs = imagesByBatch.TryGetValue(b.Id, out var ilist) ? ilist : new List<Image>();
            var batchParticipants = batchUsersByBatch.TryGetValue(b.Id, out var buList)
                ? buList.Select(u => ToUserDto(users, u.UserId))
                : Enumerable.Empty<UserDto>();
            var imgsDto = imgs.Select(i =>
            {
                var ius = imgUsersByImg.TryGetValue(i.Id, out var lst) ? lst : new List<ImageUser>();
                var usDto = ius.Select(u => ToUserDto(users, u.UserId));
                return new ImageDto(i.Id, i.Name, usDto);
            });
            return new BatchDto(b.Id, b.Name, imgsDto, batchParticipants);
        });

        return Ok(ApiResponse.Success(data, "Batches retrieved successfully."));
    }

    [HttpPost("{id:long}/batches")]
    public async Task<ActionResult<ApiResponse<object>>> CreateBatch(long id, [FromBody] Projects.Api.Contracts.Batches.CreateBatchRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            return BadRequest(ApiResponse.Failure<object>("Batch name is required."));
        }

        var projectExists = await _db.Projects.AnyAsync(p => p.Id == id, ct);
        if (!projectExists)
        {
            return NotFound(ApiResponse.Failure<object>("Project not found."));
        }

        var batch = new Shared.Domain.Batches.Batch { Name = req.Name.Trim(), ProjectId = id };
        await _db.Batches.AddAsync(batch, ct);
        await _db.SaveChangesAsync(ct);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Success<object>(new { batch.Id, batch.Name }, "Batch created successfully."));
    }

    [HttpDelete("{projectId:long}/batches/{batchId:long}")]
    public async Task<ActionResult<ApiResponse<object?>>> DeleteBatch(long projectId, long batchId, CancellationToken ct)
    {
        var batch = await _db.Batches.FirstOrDefaultAsync(x => x.Id == batchId && x.ProjectId == projectId, ct);
        if (batch == null) return NotFound(ApiResponse.Failure<object?>("Batch not found."));
        _db.Batches.Remove(batch);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Success<object?>(null, "Batch deleted successfully."));
    }

    private ParticipantRole ParseRole(string role)
    {
        return role?.ToLower() switch
        {
            "modeller" => ParticipantRole.Modeller,
            "artist" => ParticipantRole.Artist,
            "art_director" => ParticipantRole.Art_Director,
            "project_manager" => ParticipantRole.Project_Manager,
            _ => ParticipantRole.Freelancer
        };
    }

    private async Task<ProjectDto?> LoadProjectDto(long id, CancellationToken ct)
    {
        var p = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p == null) return null;

        var batches = await _db.Batches.AsNoTracking().Where(b => b.ProjectId == id).ToListAsync(ct);
        var batchIds = batches.Select(b => b.Id).ToArray();
        var images = await _db.Images.AsNoTracking().Where(i => batchIds.Contains(i.BatchId)).ToListAsync(ct);
        var imageIds = images.Select(i => i.Id).ToArray();

        var projectUsers = await _db.ProjectUsers.AsNoTracking().Where(x => x.ProjectId == id).ToListAsync(ct);
        var batchUsers = await _db.BatchUsers.AsNoTracking().Where(x => batchIds.Contains(x.BatchId)).ToListAsync(ct);
        var imageUsers = await _db.ImageUsers.AsNoTracking().Where(x => imageIds.Contains(x.ImageId)).ToListAsync(ct);
        var userIds = projectUsers.Select(x => x.UserId)
            .Concat(batchUsers.Select(x => x.UserId))
            .Concat(imageUsers.Select(x => x.UserId)).Distinct().ToArray();
        var users = await _db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, ct);

        var batchUsersByBatch = batchUsers.GroupBy(bu => bu.BatchId).ToDictionary(g => g.Key, g => g.ToList());
        var imagesByBatch = images.GroupBy(i => i.BatchId).ToDictionary(g => g.Key, g => g.ToList());
        var imgUsersByImg = imageUsers.GroupBy(iu => iu.ImageId).ToDictionary(g => g.Key, g => g.ToList());

        var batchesDto = batches.Select(b =>
        {
            var imgs = imagesByBatch.TryGetValue(b.Id, out var ilist) ? ilist : new List<Image>();
            var batchParticipants = batchUsersByBatch.TryGetValue(b.Id, out var buList)
                ? buList.Select(u => ToUserDto(users, u.UserId))
                : Enumerable.Empty<UserDto>();
            var imgsDto = imgs.Select(i =>
            {
                var ius = imgUsersByImg.TryGetValue(i.Id, out var lst) ? lst : new List<ImageUser>();
                var iusDto = ius.Select(u => ToUserDto(users, u.UserId));
                return new ImageDto(i.Id, i.Name, iusDto);
            });
            return new BatchDto(b.Id, b.Name, imgsDto, batchParticipants);
        });

        var pUsersDto = projectUsers.Select(u => ToUserDto(users, u.UserId));

        return new ProjectDto(
            p.Id,
            p.Name,
            p.IsActive,
            p.StartDate,
            p.EndDate,
            p.ClientName,
            p.DeadlineType.ToString().ToLowerInvariant(),
            batchesDto,
            pUsersDto);
    }

    private static UserDto ToUserDto(IReadOnlyDictionary<long, User> users, long userId)
    {
        if (!users.TryGetValue(userId, out var user))
        {
            return new UserDto(userId, "unknown", "unknown");
        }

        return new UserDto(user.Id, user.Name, user.Role);
    }
}


