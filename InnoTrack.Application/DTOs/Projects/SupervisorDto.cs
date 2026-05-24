namespace InnoTrack.Application.DTOs.Projects
{
    public record SupervisorDto(
        int Id,
        string FullName,
        string DepartmentName,
        string Email,
        int CurrentTeamLoad,
        int MaxTeamLoad
    )
    {
        public bool IsAvailable => CurrentTeamLoad < MaxTeamLoad;
    }
}
