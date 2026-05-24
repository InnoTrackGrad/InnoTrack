namespace InnoTrack.Application.DTOs.Projects
{
    public record SubmitProjectRequestDto(int SupervisorId, string Department, string TeamMembers, string Message);
}
