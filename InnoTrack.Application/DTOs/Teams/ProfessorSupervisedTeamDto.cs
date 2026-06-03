namespace InnoTrack.Application.DTOs.Teams
{
    public record ProfessorSupervisedTeamDto(
        int Id,
        string Name,
        int? ProjectId,
        string? ProjectTitle,
        string JoinCode,
        int Progress,
        IReadOnlyList<TeamMemberDetailDto> Members
    );
}
