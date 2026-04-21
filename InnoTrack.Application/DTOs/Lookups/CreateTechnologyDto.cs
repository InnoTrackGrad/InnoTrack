using InnoTrack.Domain.Entities.Enums;

namespace InnoTrack.Application.DTOs.Lookups
{
    public record CreateTechnologyDto(string Name, TechnologyCategory Category);
}
