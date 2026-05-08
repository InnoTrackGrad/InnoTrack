namespace InnoTrack.Application.DTOs.AI
{
    public record SimilarProjectDto(int ReferencedProjectId, decimal SimilarityPercentage, string MatchReason);
}
