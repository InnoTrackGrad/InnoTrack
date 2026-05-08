using InnoTrack.Application.DTOs.Students;

namespace InnoTrack.Application.Interfaces
{
    public interface IStudentService
    {
        Task<StudentProfileDto> GetStudentProfileAsync(int userId);
        Task<StudentPublicProfileDto> GetPublicStudentProfileAsync(int studentId);
    }
}