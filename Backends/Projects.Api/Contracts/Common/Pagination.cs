namespace Projects.Api.Contracts.Common;

public record Pagination(int CurrentPage, int PerPage, int Total, int LastPage);

public record PagedResponse<T>(bool Success, IEnumerable<T> Data, Pagination Pagination, string? Message = null);

public record ApiResponse<T>(bool Success, T? Data, string? Message = null, object? Meta = null);

public static class ApiResponse
{
    public static ApiResponse<T> Success<T>(T data, string? message = null, object? meta = null) =>
        new(true, data, message, meta);

    public static ApiResponse<T> Failure<T>(string message, object? meta = null) =>
        new(false, default, message, meta);
}


