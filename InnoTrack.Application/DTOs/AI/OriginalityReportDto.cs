using InnoTrack.Application.DTOs.Projects;

namespace InnoTrack.Application.DTOs.AI
{
    public record OriginalityReportDto(
        int ProjectId, decimal OverallScore, string Summary, DateTime GeneratedAt,
        IReadOnlyList<SimilarProjectResultDto> SimilarProjects
    );
}
