using InnoTrack.Application.Common;
using InnoTrack.Application.DTOs.Projects;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Entities.Enums;
using InnoTrack.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InnoTrack.Application.Services
{
    public class ProfessorProjectService : IProfessorProjectService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        public ProfessorProjectService(IUnitOfWork unitOfWork, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        public async Task<PagedResult<ProfessorPendingProjectDto>> GetPendingProjectsAsync(int professorId, int pageNumber, int pageSize)
        {
            var professor = await _unitOfWork.Repository<Professor>().GetByIdAsync(professorId);
            if (professor == null) throw new KeyNotFoundException("Professor not found.");

            var query = _unitOfWork.Repository<Project>().GetQueryable()
                .AsNoTracking()
                .Where(p => p.Status == ProjectStatus.UnderReview && p.Team.ProfessorId == professorId);

            var totalCount = await query.CountAsync();

            var pendingProjects = await query
                .OrderBy(p => p.SubmittedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProfessorPendingProjectDto(
                    p.Id,
                    p.Title,
                    p.Abstract,
                    p.OriginalityScore,
                    p.Status.ToString()
                ))
                .ToListAsync();

            return new PagedResult<ProfessorPendingProjectDto>(pendingProjects, totalCount, pageNumber, pageSize);
        }

        public async Task ReviewProjectAsync(int professorId, int projectId, bool approve)
        {

            var project = await _unitOfWork.Repository<Project>().GetByIdAsync(projectId);
            if (project == null)
                throw new KeyNotFoundException("Project not found.");

            var team = await _unitOfWork.Repository<Team>().GetByIdAsync(project.TeamId);
            if (team is null || team.ProfessorId != professorId)
                throw new UnauthorizedAccessException("You are not authorized to review this project.");

            if (project.Status != ProjectStatus.UnderReview)
                throw new InvalidOperationException("Project is not currently submitted for review.");

            project.Status = approve ? ProjectStatus.In_Progress : ProjectStatus.Rejected;
            project.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Project>().Update(project);
            await _unitOfWork.CompleteAsync();

            var teamMembers = await _unitOfWork.Repository<TeamMember>()
                .GetAllAsync(tm => tm.TeamId == project.TeamId);

            var statusStr = approve ? "Approved" : "Rejected";
            var message = $"Your project '{project.Title}' has been {statusStr} by the professor.";
            var notifType = approve ? NotificationType.Success : NotificationType.Error;

            if (teamMembers != null && teamMembers.Any())
            {
                foreach (var member in teamMembers)
                {
                    await _notificationService.SendNotificationAsync(
                        member.StudentId,
                        "Project Review Updated",
                        message,
                        notifType);
                }
            }

        }
    }
}