using System.Text.Json;
using System.Text.Json.Serialization;

namespace InnoTrack.Application.DTOs.AI
{
    public class PythonSimilarProjectDto
    {
        [JsonPropertyName("project_title")]
        public string ProjectTitle { get; set; } = string.Empty;

        [JsonPropertyName("matched_features")]
        public List<JsonElement> MatchedFeatures { get; set; } = new();

        [JsonPropertyName("unique_features")]
        public List<JsonElement> UniqueFeatures { get; set; } = new();

        [JsonPropertyName("similarity_score")]
        public decimal SimilarityScore { get; set; }

        [JsonPropertyName("final_originality_score")]
        public decimal FinalOriginalityScore { get; set; }
    }
}
