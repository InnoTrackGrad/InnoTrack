namespace InnoTrack.Application.DTOs.Projects
{
    public record ProjectCatalogItemDto(
        int Id,
        string Title,
        string Domain,
        string Status,
        int Year,
        string? Supervisor,
        IReadOnlyList<string> Students,
        IReadOnlyList<string> Technologies,
        decimal? OriginalityScore
    );
}