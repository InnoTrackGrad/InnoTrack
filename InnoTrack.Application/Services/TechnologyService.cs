using AutoMapper;
using InnoTrack.Application.Common;
using InnoTrack.Application.DTOs.Lookups;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Entities.Enums;
using InnoTrack.Domain.Interfaces;

namespace InnoTrack.Application.Services
{
    public class TechnologyService : ITechnologyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public TechnologyService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<TechnologyDto> CreateTechnologyAsync(CreateTechnologyDto request)
        {
            if (!Enum.TryParse<TechnologyCategory>(request.Category, true, out var categoryEnum))
                throw new ArgumentException($"Invalid category: {request.Category}");
            var technology = new Technology { Name = request.Name, Category = categoryEnum };
            await _unitOfWork.Repository<Technology>().AddAsync(technology);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<TechnologyDto>(technology);
        }

        public async Task<PagedResult<TechnologyDto>> GetAllTechnologiesAsync(int pageNumber, int pageSize)
        {
            var (data, totalCount) = await _unitOfWork.Repository<Technology>().GetPagedAsync(
                pageNumber: pageNumber,
                pageSize: pageSize);

            var mappedData = _mapper.Map<IReadOnlyList<TechnologyDto>>(data);
            return new PagedResult<TechnologyDto>(mappedData, totalCount, pageNumber, pageSize);
        }

        public async Task<TechnologyDto> GetTechnologyByIdAsync(int id)
        {
            var technology = await _unitOfWork.Repository<Technology>().GetByIdAsync(id);
            if (technology == null) throw new KeyNotFoundException("Technology not found.");
            return _mapper.Map<TechnologyDto>(technology);
        }
        public async Task UpdateTechnologyAsync(int id, CreateTechnologyDto request)
        {
            var technology = await _unitOfWork.Repository<Technology>().GetByIdAsync(id);
            if (technology == null) throw new KeyNotFoundException("Technology not found.");

            if (!Enum.TryParse<TechnologyCategory>(request.Category, true, out var categoryEnum))
                throw new ArgumentException($"Invalid category: {request.Category}");
           
            technology.Name = request.Name;
            technology.Category = categoryEnum;

            _unitOfWork.Repository<Technology>().Update(technology);
            await _unitOfWork.CompleteAsync();
        }

        public async Task DeleteTechnologyAsync(int id)
        {
            var technology = await _unitOfWork.Repository<Technology>().GetByIdAsync(id);
            if (technology == null) throw new KeyNotFoundException("Technology not found.");
            _unitOfWork.Repository<Technology>().Delete(technology);
            await _unitOfWork.CompleteAsync();
        }
    }
}
