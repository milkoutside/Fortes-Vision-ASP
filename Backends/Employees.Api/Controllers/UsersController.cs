using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Employees.Api.Contracts.Common;
using Shared.Database;

namespace Employees.Api.Controllers;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly SharedDbContext _db;
    public UsersController(SharedDbContext db) { _db = db; }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<UserResponse>>> Index([FromQuery] string? role, [FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var q = _db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(role) && role != "all") q = q.Where(u => u.Role == role);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            q = q.Where(u => EF.Functions.Like(u.Name, term));
        }
        q = q.OrderBy(u => u.Id);
        var total = await q.CountAsync(ct);
        var perPage = Math.Max(1, limit);
        var currentPage = Math.Max(1, page);
        var skip = (currentPage - 1) * perPage;
        var users = await q.Skip(skip).Take(perPage).AsNoTracking().ToListAsync(ct);
        var data = users.Select(u => new UserResponse(u.Id, u.Name, u.Role));
        var pagination = new Pagination(currentPage, perPage, total, (int)Math.Ceiling(total / (double)perPage));
        return Ok(new PagedResponse<UserResponse>(true, data, pagination, "Users retrieved successfully."));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetById(long id, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user == null) return NotFound(ApiResponse.Failure<UserResponse>("User not found."));
        return Ok(ApiResponse.Success(new UserResponse(user.Id, user.Name, user.Role), "User retrieved successfully."));
    }

    public record UpsertUserRequest(string Name, string Role);
    public record UserResponse(long Id, string Name, string Role);

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Create([FromBody] UpsertUserRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Role))
            return BadRequest(ApiResponse.Failure<UserResponse>("Name and role are required."));
        var u = new Shared.Domain.Users.User { Name = req.Name.Trim(), Role = req.Role.Trim() };
        await _db.Users.AddAsync(u, ct);
        await _db.SaveChangesAsync(ct);
        return StatusCode(201, ApiResponse.Success(new UserResponse(u.Id, u.Name, u.Role), "User created successfully."));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Update(long id, [FromBody] UpsertUserRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Role))
            return BadRequest(ApiResponse.Failure<UserResponse>("Name and role are required."));
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (u == null) return NotFound(ApiResponse.Failure<UserResponse>("User not found."));
        u.Name = req.Name.Trim();
        u.Role = req.Role.Trim();
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Success(new UserResponse(u.Id, u.Name, u.Role), "User updated successfully."));
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(long id, CancellationToken ct)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (u == null) return NotFound(ApiResponse.Failure<object?>("User not found."));
        _db.Users.Remove(u);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Success<object?>(null, "User deleted successfully."));
    }
}

