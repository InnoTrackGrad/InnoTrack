namespace InnoTrack.Application.DTOs.Projects
{
    public record ProjectCatalogDetailDto(
        int Id,
        string Title,
        string Domain,
        string Status,
        int Year,
        string? Supervisor,
        IReadOnlyList<string> Technologies,
        decimal? OriginalityScore,
        DateTime? SubmittedAt,
        DateTime? UpdatedAt,
        string Description,
        string Abstract,
        IReadOnlyList<StudentInProjectDto> Students
    );

    public record StudentInProjectDto(
        string Name,
        string Role,
        string Department
    );
}