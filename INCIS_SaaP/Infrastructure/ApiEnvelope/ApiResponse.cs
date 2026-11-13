namespace INCIS_SaaP.Infrastructure.ApiEnvelope;

/// <summary>
/// Standardized API response envelope
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public PaginationMeta? Pagination { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message
    };

    public static ApiResponse<T> Paginated(T data, int totalItems, int pageIndex, int pageSize) => new()
    {
        Success = true,
        Data = data,
        Pagination = new PaginationMeta
        {
            TotalItems = totalItems,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
            HasPreviousPage = pageIndex > 1,
            HasNextPage = pageIndex < (int)Math.Ceiling(totalItems / (double)pageSize)
        }
    };

    public static ApiResponse<T> Fail(string message) => new()
    {
        Success = false,
        Message = message
    };
}

/// <summary>
/// Pagination metadata for list responses
/// </summary>
public class PaginationMeta
{
    public int TotalItems { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}
