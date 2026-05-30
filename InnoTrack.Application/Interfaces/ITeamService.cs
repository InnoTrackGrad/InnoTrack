using InnoTrack.Application.DTOs.Teams;

namespace InnoTrack.Application.Interfaces
{
    public interface ITeamService
    {
        Task<TeamResponseDto> CreateTeamAsync(int leaderStudentId, CreateTeamDto dto);
        Task<GenerateJoinCodeResponseDto> RegenerateJoinCodeAsync(int userId);
        Task<DirectJoinResponseDto> DirectJoinByCodeAsync(int userId, string joinCode);
        Task RenameTeamAsync(int userId, string newName);
        Task DeleteTeamAsync(int userId);
    }
}
