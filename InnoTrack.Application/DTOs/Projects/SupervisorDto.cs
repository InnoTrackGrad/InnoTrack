namespace InnoTrack.Application.DTOs.Projects
{
    public record SupervisorDto(
        int Id,
        string FullName,
        string DepartmentName,
        string Specialization,
        string Email,
        int CurrentTeamLoad
    );
}