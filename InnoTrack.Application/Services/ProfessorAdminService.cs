// InnoTrack.Application/Services/ProfessorAdminService.cs
using InnoTrack.Application.DTOs.Professors;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Entities.Enums;
using InnoTrack.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InnoTrack.Application.Services
{
    public class ProfessorAdminService : IProfessorAdminService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;

        public ProfessorAdminService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
        }

        public async Task<ProfessorAdminViewDto> CreateProfessorAsync(CreateProfessorDto dto)
        {
            // Email uniqueness check across ALL users, not just professors
            var existing = await _unitOfWork.Repository<User>()
                .FindAsync(u => u.Email == dto.Email.Trim().ToLower());
            if (existing is not null)
                throw new InvalidOperationException("A user with this email already exists.");

            var department = await _unitOfWork.Repository<Department>()
                .GetByIdAsync(dto.DepartmentId)
                ?? throw new KeyNotFoundException($"Department ID {dto.DepartmentId} not found.");

            var professor = new Professor
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = dto.Email.Trim().ToLower(),
                PasswordHash = _passwordHasher.Hash(dto.Password),
                DepartmentId = dto.DepartmentId,
                MaxTeamLoad = dto.MaxTeamLoad,
                Role = UserRole.Professor,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Professor>().AddAsync(professor);
            await _unitOfWork.CompleteAsync();

            return new ProfessorAdminViewDto(
                professor.Id, professor.FullName, professor.Email,
                professor.DepartmentId, department.Name,
                professor.MaxTeamLoad, CurrentTeamLoad: 0,
                professor.IsActive, professor.CreatedAt);
        }

        public async Task<IReadOnlyList<ProfessorAdminViewDto>> GetAllProfessorsAsync()
        {
            var result = await _unitOfWork.Repository<Professor>()
                .GetQueryable()
                .AsNoTracking()
                .Include(p => p.Department)
                .OrderBy(p => p.FirstName).ThenBy(p => p.LastName)
                .Select(p => new ProfessorAdminViewDto(
                    p.Id, p.FullName, p.Email,
                    p.DepartmentId, p.Department.Name,
                    p.MaxTeamLoad,
                    p.SupervisedTeams.Count,
                    p.IsActive, p.CreatedAt))
                .ToListAsync();

            return result.AsReadOnly();
        }

        public async Task<ProfessorAdminViewDto> GetProfessorByIdAsync(int professorId)
        {
            var professor = await _unitOfWork.Repository<Professor>()
                .GetQueryable()
                .AsNoTracking()
                .Include(p => p.Department)
                .FirstOrDefaultAsync(p => p.Id == professorId)
                ?? throw new KeyNotFoundException("Professor not found.");

            // CountAsync for current load — avoids loading all team entities
            var currentLoad = await _unitOfWork.Repository<Team>()
                .CountAsync(t => t.ProfessorId == professorId);

            return new ProfessorAdminViewDto(
                professor.Id, professor.FullName, professor.Email,
                professor.DepartmentId, professor.Department.Name,
                professor.MaxTeamLoad, currentLoad,
                professor.IsActive, professor.CreatedAt);
        }

        public async Task UpdateProfessorAsync(int professorId, UpdateProfessorAdminDto dto)
        {
            var professor = await _unitOfWork.Repository<Professor>().GetByIdAsync(professorId)
                ?? throw new KeyNotFoundException("Professor not found.");

            if (dto.FirstName is not null) professor.FirstName = dto.FirstName.Trim();
            if (dto.LastName is not null) professor.LastName = dto.LastName.Trim();

            if (dto.DepartmentId.HasValue)
            {
                _ = await _unitOfWork.Repository<Department>().GetByIdAsync(dto.DepartmentId.Value)
                    ?? throw new KeyNotFoundException($"Department {dto.DepartmentId} not found.");
                professor.DepartmentId = dto.DepartmentId.Value;
            }

            if (dto.MaxTeamLoad.HasValue)
            {
                var currentLoad = await _unitOfWork.Repository<Team>()
                    .CountAsync(t => t.ProfessorId == professorId);

                if (dto.MaxTeamLoad.Value < currentLoad)
                    throw new InvalidOperationException(
                        $"Cannot reduce capacity below the current team load ({currentLoad} supervised teams). " +
                        $"Reassign teams first.");

                professor.MaxTeamLoad = dto.MaxTeamLoad.Value;
            }

            if (dto.IsActive.HasValue) professor.IsActive = dto.IsActive.Value;

            _unitOfWork.Repository<Professor>().Update(professor);
            await _unitOfWork.CompleteAsync();
        }

        public async Task SetProfessorActiveStatusAsync(int professorId, bool isActive)
        {
            var professor = await _unitOfWork.Repository<Professor>().GetByIdAsync(professorId)
                ?? throw new KeyNotFoundException("Professor not found.");

            professor.IsActive = isActive;
            _unitOfWork.Repository<Professor>().Update(professor);
            await _unitOfWork.CompleteAsync();
        }

        public async Task ResetProfessorPasswordAsync(int professorId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
                throw new ArgumentException("New password must be at least 8 characters.");

            var professor = await _unitOfWork.Repository<Professor>().GetByIdAsync(professorId)
                ?? throw new KeyNotFoundException("Professor not found.");

            professor.PasswordHash = _passwordHasher.Hash(newPassword);
            _unitOfWork.Repository<Professor>().Update(professor);
            await _unitOfWork.CompleteAsync();
        }
    }
}