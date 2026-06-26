namespace AiCustomerService.Core.DTOs.Knowledge;

public record UploadDocumentRequest(string Title);

public record DocumentDto(
    Guid Id,
    string Title,
    string Status,
    int ChunkCount,
    long FileSize,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    string? ErrorMessage,
    string? JobId
);

public record ChunkDto(
    Guid Id,
    int ChunkIndex,
    string Content,
    int ContentLength,
    string Metadata
);

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;
}
