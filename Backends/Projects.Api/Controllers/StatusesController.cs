using Microsoft.AspNetCore.Mvc;
using Projects.Api.Contracts.Common;
using Projects.Api.Contracts.Statuses;
using Shared.Application.Common;
using Shared.Application.Statuses;
using Shared.Domain.Statuses;

namespace Projects.Api.Controllers;

[ApiController]
[Route("statuses")]
public class StatusesController : ControllerBase
{
    private readonly IStatusService _statusService;

    public StatusesController(IStatusService statusService)
    {
        _statusService = statusService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<StatusDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await _statusService.GetAllAsync(cancellationToken);
        var result = items.Select(MapToDto);
        return Ok(ApiResponse.Success(result, "Statuses retrieved successfully."));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<StatusDto>>> GetById(long id, CancellationToken cancellationToken)
    {
        var item = await _statusService.GetByIdAsync(id, cancellationToken);
        if (item == null) return NotFound(ApiResponse.Failure<StatusDto>("Status not found."));
        return Ok(ApiResponse.Success(MapToDto(item), "Status retrieved successfully."));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<StatusDto>>> Create([FromBody] CreateStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _statusService.CreateAsync(request.Name, request.Color, request.TextColor, cancellationToken);
            var dto = MapToDto(created);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse.Success(dto, "Status created successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Failure<StatusDto>(ex.Message));
        }
        catch (ConflictException ex)
        {
            return Conflict(ApiResponse.Failure<StatusDto>(ex.Message));
        }
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<StatusDto>>> Update(long id, [FromBody] UpdateStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _statusService.UpdateAsync(id, request.Name, request.Color, request.TextColor, cancellationToken);
            if (updated == null) return NotFound(ApiResponse.Failure<StatusDto>("Status not found."));
            return Ok(ApiResponse.Success(MapToDto(updated), "Status updated successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Failure<StatusDto>(ex.Message));
        }
        catch (ConflictException ex)
        {
            return Conflict(ApiResponse.Failure<StatusDto>(ex.Message));
        }
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(long id, CancellationToken cancellationToken)
    {
        var ok = await _statusService.DeleteAsync(id, cancellationToken);
        if (!ok) return NotFound(ApiResponse.Failure<object?>("Status not found."));
        return Ok(ApiResponse.Success<object?>(null, "Status deleted successfully."));
    }

    private static StatusDto MapToDto(Status s) => new StatusDto(s.Id, s.Name, s.Color, s.TextColor);
}


