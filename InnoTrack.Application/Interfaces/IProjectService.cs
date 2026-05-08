using InnoTrack.Application.DTOs.Projects;

namespace InnoTrack.Application.Interfaces
{
    public interface IProjectService
    {
        Task<ProjectResponseDto> CreateProjectAsync(int leaderId, CreateProjectDto dto);
        Task VerifyProjectForSubmissionAsync(int projectId, int userId);
    }
}
