using InnoTrack.Application.DTOs.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.Interfaces
{
    public interface IPythonAiClient
    {
        Task<PythonAiResponseDto> AnalyzeProjectAsync(PythonAiRequestDto request);
    }
}
