using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Employees.Api.Contracts.Common;
using Shared.Database;

namespace Employees.Api.Controllers;

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
    public async Task<ActionResult<PagedResponse<ProjectDto>>> Index([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var projectQuery = _db.Projects.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var like = $"%{term}%";
            projectQuery = projectQuery.Where(p =>
                EF.Functions.Like(p.Name, like) ||
                (p.ClientName != null && EF.Functions.Like(p.ClientName, like)));
        }

        var perPage = Math.Max(1, limit);
        var currentPage = Math.Max(1, page);
        var total = await projectQuery.CountAsync(ct);
        var skip = (currentPage - 1) * perPage;

        var projects = await projectQuery
            .OrderBy(p => p.Name)
            .Skip(skip)
            .Take(perPage)
            .AsNoTracking()
            .ToListAsync(ct);

        var data = projects.Select(p => new ProjectDto(
            p.Id,
            p.Name,
            p.ClientName,
            p.IsActive
        ));

        var pagination = new Pagination(currentPage, perPage, total, (int)Math.Ceiling(total / (double)perPage));
        return Ok(new PagedResponse<ProjectDto>(true, data, pagination, "Projects retrieved successfully."));
    }

    public record ProjectDto(long Id, string Name, string? ClientName, bool IsActive);
}

