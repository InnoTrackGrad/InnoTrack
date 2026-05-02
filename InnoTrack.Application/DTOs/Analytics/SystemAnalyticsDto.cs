using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.DTOs.Analytics
{
    public record SystemAnalyticsDto
        (int TotalUsers, int TotalTeams, int TotalProjects, decimal AverageOriginalityScore);
}
