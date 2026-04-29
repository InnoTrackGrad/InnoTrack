using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.DTOs.AI
{
    public record SimilarProjectDto(int ReferencedProjectId, decimal SimilarityPercentage, string MatchReason);
}
