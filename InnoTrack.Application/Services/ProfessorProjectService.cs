using InnoTrack.Application.Common;
using InnoTrack.Application.DTOs.Projects;
using InnoTrack.Application.DTOs.Teams;
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
                .Where(p => p.Status == ProjectStatus.UnderReview && p.Team != null && p.Team.ProfessorId == professorId);

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

            if (!project.TeamId.HasValue) throw new InvalidOperationException("Project does not belong to a team.");
            var team = await _unitOfWork.Repository<Team>().GetByIdAsync(project.TeamId.Value);
            if (team is null || team.ProfessorId != professorId)
                throw new UnauthorizedAccessException("You are not authorized to review this project.");

            if (project.Status != ProjectStatus.UnderReview)
                throw new InvalidOperationException("Project is not currently submitted for review.");

            project.Status = approve ? ProjectStatus.In_Progress : ProjectStatus.Rejected;
            project.UpdatedAt = DateTime.UtcNow;

            if (approve)
            {
                project.ApprovedAt = DateTime.UtcNow;
            }

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
                        notifType,
                        project.Id,
                        ReferenceType.Project);
                }
            }

        }

        public async Task<IReadOnlyList<ProfessorSupervisedProjectDto>> GetSupervisedProjectsAsync(int professorId)
        {
            var projects = await _unitOfWork.Repository<Project>().GetQueryable()
                .AsNoTracking()
                .Include(p => p.Team)
                .Include(p => p.ProjectTechnologies).ThenInclude(pt => pt.Technology)
                .Where(p => p.Team != null && p.Team.ProfessorId == professorId)
                .ToListAsync();

            return projects.Select(p => new ProfessorSupervisedProjectDto(
                p.Id,
                p.Title,
                p.Team?.Name,
                p.Description,
                p.Abstract,
                p.Domain,
                p.ProjectTechnologies?.Select(pt => pt.Technology?.Name ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>(),
                p.Status.ToString(),
                p.OriginalityScore,
                p.SubmittedAt,
                p.Progress
            )).ToList().AsReadOnly();
        }

        public async Task<IReadOnlyList<ProfessorSupervisedTeamDto>> GetSupervisedTeamsAsync(int professorId)
        {
            var teams = await _unitOfWork.Repository<Team>().GetQueryable()
                .AsNoTracking()
                .Include(t => t.Project)
                .Include(t => t.TeamMembers)
                    .ThenInclude(tm => tm.Student)
                        .ThenInclude(s => s.StudentSkills)
                            .ThenInclude(ss => ss.Skill)
                .Where(t => t.ProfessorId == professorId)
                .ToListAsync();

            return teams.Select(t => new ProfessorSupervisedTeamDto(
                t.Id,
                t.Name,
                t.Project?.Id,
                t.Project?.Title,
                t.JoinCode,
                t.Project?.Progress ?? 0,
                t.TeamMembers.Where(tm => tm.Student != null).Select(tm => new TeamMemberDetailDto(
                    tm.StudentId,
                    tm.Student!.FullName,
                    tm.Role.ToString(),
                    tm.Student.Email ?? "",
                    tm.Student.StudentSkills?.Select(sk => sk.Skill?.Name ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>()
                )).ToList()
            )).ToList().AsReadOnly();
        }
    }
}