using System.Text.Json.Serialization;

namespace InnoTrack.Application.DTOs.AI
{
    public class PythonAiResponseDto
    {
        [JsonPropertyName("extracted_features")]
        public List<string> ExtractedFeatures { get; set; } = new();

        [JsonPropertyName("top_similar_projects")]
        public List<PythonSimilarProjectDto> TopSimilarProjects { get; set; } = new();
    }
}