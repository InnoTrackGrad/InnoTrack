using InnoTrack.Application.Common;
using InnoTrack.Application.DTOs.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.Interfaces
{
    public interface IProfessorProjectService
    {
        Task<PagedResult<ProfessorPendingProjectDto>> GetPendingProjectsAsync(int professorId, int pageNumber, int pageSize);
        Task ReviewProjectAsync(int professorId, int projectId, bool approve);
    }
}
