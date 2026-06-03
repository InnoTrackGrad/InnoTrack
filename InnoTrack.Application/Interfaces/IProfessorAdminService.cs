using InnoTrack.Application.DTOs.Professors;

namespace InnoTrack.Application.Interfaces
{
    public interface IProfessorAdminService
    {
        Task<ProfessorAdminViewDto> CreateProfessorAsync(CreateProfessorDto dto);
        Task<IReadOnlyList<ProfessorAdminViewDto>> GetAllProfessorsAsync();
        Task<ProfessorAdminViewDto> GetProfessorByIdAsync(int professorId);
        Task UpdateProfessorAsync(int professorId, UpdateProfessorAdminDto dto);
        Task SetProfessorActiveStatusAsync(int professorId, bool isActive);
        Task ResetProfessorPasswordAsync(int professorId, string newPassword);
    }
}