using InnoTrack.Application.DTOs.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.Interfaces
{
    public interface IProjectAnalysisService
    {
        Task ProcessProjectAiReportAsync(int projectId);
    }
}
