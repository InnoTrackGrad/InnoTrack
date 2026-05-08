namespace InnoTrack.Application.DTOs.Projects
{
    public record UpdateProjectDetailsDto(
        string? Title,
        string? Description,
        List<int>? TechnologyIds
    );
}