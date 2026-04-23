using AutoMapper;
using InnoTrack.Application.DTOs.Lookups;
using InnoTrack.Domain.Entities;

namespace InnoTrack.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Department, DepartmentDto>().ReverseMap();
            CreateMap<Domain.Entities.Domain, DomainDto>().ReverseMap();
            CreateMap<Technology, TechnologyDto>()
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category.ToString()));
        }
    }
}
