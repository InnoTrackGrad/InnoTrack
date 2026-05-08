namespace InnoTrack.Application.DTOs.AI
{
    public record PythonAiResponseDto(
        decimal OriginalityScore,
        string Summary,
        string VectorData,
        List<SimilarProjectDto> SimilarProjects);
}
