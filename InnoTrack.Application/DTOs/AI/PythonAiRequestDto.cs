using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.DTOs.AI
{
    public record PythonAiRequestDto(int ProjectId, string Title, string Abstract, string Description);
}