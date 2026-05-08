using InnoTrack.Application.Common;
using InnoTrack.Application.DTOs.Projects;

namespace InnoTrack.Application.Interfaces
{
    public interface IProjectCatalogService
    {
        Task<PagedResult<ProjectCatalogItemDto>> GetProjectsAsync(
            int? year, string? status, string? search, int pageNumber, int pageSize);
        Task<ProjectCatalogDetailDto> GetProjectByIdAsync(int projectId);
        Task<MyProjectResponseDto?> GetMyProjectAsync(int userId);
        Task<IReadOnlyList<SupervisorDto>> GetSupervisorsAsync();
        Task<SaveDraftResponseDto> SaveDraftAsync(int userId, SaveProjectDraftDto dto);
        Task<SaveDraftResponseDto> UpdateDraftAsync(int draftId, int userId, SaveProjectDraftDto dto);
        Task DeleteDraftAsync(int draftId, int userId);
        Task UpdateProjectDetailsAsync(int projectId, int userId, UpdateProjectDetailsDto dto);
        Task RecallSubmissionAsync(int projectId, int userId);
        Task<SimilarityCheckResponseDto> RunSimilarityCheckAsync(SimilarityCheckRequestDto dto);
    }
}