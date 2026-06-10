using InnoTrack.Application.Common;
using InnoTrack.Application.DTOs.Admin;

namespace InnoTrack.Application.Interfaces
{
    public interface IAcademicYearService
    {
        Task<AcademicYearDto> CreateAsync(CreateAcademicYearDto dto);
        Task<PagedResult<AcademicYearDto>> GetAllAsync(int pageNumber, int pageSize);
        Task<AcademicYearDto?> GetActiveAsync();
        Task ActivateAsync(int academicYearId);
        Task UpdateAsync(int academicYearId, UpdateAcademicYearDto dto);
    }
}