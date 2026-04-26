using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.DTOs.Projects
{
    public record CreateProjectDto
        (string Title,
        string Abstract,
        string Description,
        int DomainId,
        List<int> TechnologyIds);
}
