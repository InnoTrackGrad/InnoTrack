namespace InnoTrack.Application.DTOs.Projects
{
    public record CreateProjectDto
        (string Title,
        string Abstract,
        string Description,
        int DomainId,
        List<int> TechnologyIds);
}
