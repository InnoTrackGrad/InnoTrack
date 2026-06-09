using InnoTrack.Application.Common;
using InnoTrack.Application.DTOs.Professors;
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

        /// <summary>
        /// Maps project lifecycle status to a meaningful progress percentage.
        /// Used in both list and detail views so progress is never hardcoded.
        /// </summary>
        private static int ToProgressPercent(ProjectStatus status) => status switch
        {
            ProjectStatus.Draft => 10,
            ProjectStatus.UnderReview => 30,
            ProjectStatus.Approved => 50,
            ProjectStatus.In_Progress => 75,
            ProjectStatus.Completed => 100,
            _ => 0   // Rejected, Abandoned
        };

        /// <summary>
        /// Sends a notification to all team members without letting a failure abort the
        /// primary business operation. Notification errors are swallowed and should be
        /// observed via ILogger in production.
        /// </summary>
        private async Task NotifyTeamMembersAsync(
            int teamId, string title, string message,
            NotificationType type, int referenceId)
        {
            var teamMembers = await _unitOfWork.Repository<TeamMember>()
                .GetAllAsync(tm => tm.TeamId == teamId);

            foreach (var member in teamMembers)
            {
                try
                {
                    await _notificationService.SendNotificationAsync(
                        member.StudentId, title, message, type,
                        referenceId, ReferenceType.Project);
                }
                catch { /* notification failure must never roll back the primary operation */ }
            }
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

            var team = await _unitOfWork.Repository<Team>().GetByIdAsync(project.TeamId.Value)
                ?? throw new KeyNotFoundException("Team not found.");

            if (team.ProfessorId != professorId)
                throw new UnauthorizedAccessException(
                    "You are not authorized to review this project.");

            if (project.Status != ProjectStatus.UnderReview)
                throw new InvalidOperationException(
                    "Only projects with 'UnderReview' status can be reviewed.");

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

            var verdict = approve ? "Approved" : "Rejected";
            var message = $"Your project '{project.Title}' has been {verdict} by the supervisor.";
            var notifType = approve ? NotificationType.Success : NotificationType.Error;

            await NotifyTeamMembersAsync(project.TeamId.Value, "Project Review Update",
                message, notifType, project.Id);

            // Log activity
            var professor = await _unitOfWork.Repository<Professor>().GetByIdAsync(professorId);
            var log = new ProjectActivityLog
            {
                ProjectId = projectId,
                Type = "status",
                Message = approve
                    ? "Proposal approved — project moved to In Progress"
                    : "Proposal rejected by supervisor",
                ActorName = professor?.FullName ?? "Supervisor",
                IconName = approve ? "GitCommit" : "History",
                ColorClass = approve ? "text-emerald-500" : "text-red-500",
                BgClass = approve ? "bg-emerald-500/10" : "bg-red-500/10"
            };
            await _unitOfWork.Repository<ProjectActivityLog>().AddAsync(log);
            await _unitOfWork.CompleteAsync();

            await _notificationService.SendProjectActivityLogAsync(projectId, log.Type, log.Message, log.ActorName, log.IconName, log.ColorClass, log.BgClass);
        }

        public async Task<IReadOnlyList<ProfessorSupervisedProjectDto>> GetSupervisedProjectsAsync(
            int professorId)
        {
            var projects = await _unitOfWork.Repository<Project>()
                .GetQueryable()
                .AsNoTracking()
                .Include(p => p.Team)
                .Include(p => p.Domain)
                .Include(p => p.ProjectTechnologies)
                    .ThenInclude(pt => pt.Technology)
                .Where(p => p.Team != null && p.Team.ProfessorId == professorId &&
                            (p.Status == ProjectStatus.In_Progress || p.Status == ProjectStatus.Approved))
                .OrderByDescending(p => p.SubmittedAt ?? p.CreatedAt)
                .ToListAsync();

            var projectIds = projects.Select(p => p.Id).ToList();
            var mutedProjectIds = await _unitOfWork.Repository<ProjectNotificationPreference>()
                .GetQueryable()
                .AsNoTracking()
                .Where(pref => pref.UserId == professorId && projectIds.Contains(pref.ProjectId) && pref.IsMuted)
                .Select(pref => pref.ProjectId)
                .ToListAsync();

            return projects
                .Select(p => new ProfessorSupervisedProjectDto(
                    p.Id, p.Title,
                    p.Team?.Name,
                    p.Description,
                    p.Abstract,
                    p.Domain?.Name ?? string.Empty,
                    p.ProjectTechnologies
                        .Select(pt => pt.Technology?.Name ?? string.Empty)
                        .Where(n => n.Length > 0)
                        .ToList()
                        .AsReadOnly(),
                    p.Status.ToString(),
                    p.OriginalityScore,
                    p.SubmittedAt,
                    ToProgressPercent(p.Status),
                    mutedProjectIds.Contains(p.Id)
                ))
                .ToList()
                .AsReadOnly();
        }

        public async Task<IReadOnlyList<ProfessorSupervisedTeamDto>> GetSupervisedTeamsAsync(
            int professorId)
        {
            var teams = await _unitOfWork.Repository<Team>()
                .GetQueryable()
                .AsNoTracking()
                .Include(t => t.Project)
                .Include(t => t.Members)
                    .ThenInclude(tm => tm.Student)
                        .ThenInclude(s => s.StudentSkills)
                            .ThenInclude(ss => ss.Skill)
                .Where(t => t.ProfessorId == professorId &&
                        t.Project != null &&
                        (t.Project.Status == ProjectStatus.In_Progress || t.Project.Status == ProjectStatus.Approved))
                .OrderBy(t => t.Name)
                .ToListAsync();

            return teams
                .Select(t => new ProfessorSupervisedTeamDto(
                    t.Id,
                    t.Name,
                    t.Members.Count,
                    t.Project?.Id,
                    t.Project?.Title,
                    t.Project?.Status.ToString(),
                    t.Project?.OriginalityScore,
                    t.Project?.SubmittedAt,
                    t.Project?.Status == ProjectStatus.UnderReview,
                    t.Members
                        .Where(tm => tm.Student != null)
                        .Select(tm => new TeamMemberDetailDto(
                            tm.StudentId,
                            tm.Student!.FullName,
                            tm.Role.ToString(),
                            tm.Student.Email ?? string.Empty,
                            tm.Student.StudentSkills
                                .Select(sk => sk.Skill?.Name ?? string.Empty)
                                .Where(n => n.Length > 0)
                                .ToList()
                                .AsReadOnly()
                        ))
                        .ToList()
                        .AsReadOnly()
                ))
                .ToList()
                .AsReadOnly();
        }

        public async Task<ProfessorProjectDetailDto> GetProjectDetailAsync(
            int professorId, int projectId)
        {
            var project = await _unitOfWork.Repository<Project>()
                .GetQueryable()
                .AsNoTracking()
                .Include(p => p.Team)
                .Include(p => p.Domain)
                .Include(p => p.AcademicYear)
                .Include(p => p.ProjectTechnologies)
                    .ThenInclude(pt => pt.Technology)
                .Include(p => p.Feedbacks)
                .FirstOrDefaultAsync(p => p.Id == projectId)
                ?? throw new KeyNotFoundException("Project not found.");

            if (project.Team?.ProfessorId != professorId)
                throw new UnauthorizedAccessException(
                    "You are not authorized to view this project.");

            // Load members and their full student profiles via bulk query (avoids N+1)
            var teamMembers = await _unitOfWork.Repository<TeamMember>()
                .GetAllAsync(tm => tm.TeamId == project.TeamId);

            var studentIds = teamMembers.Select(m => m.StudentId).ToList();

            var students = await _unitOfWork.Repository<Student>()
                .GetQueryable()
                .AsNoTracking()
                .Include(s => s.StudentSkills)
                    .ThenInclude(ss => ss.Skill)
                .Where(s => studentIds.Contains(s.Id))
                .ToListAsync();

            var studentMap = students.ToDictionary(s => s.Id);

            var memberDtos = teamMembers
                .OrderBy(m => m.Role) // Leader first (enum order)
                .Select(m =>
                {
                    var s = studentMap.GetValueOrDefault(m.StudentId);
                    return new ProjectTeamMemberDto(
                        m.StudentId,
                        s?.FullName ?? "Unknown",
                        s?.Email ?? string.Empty,
                        m.Role.ToString(),
                        s?.GPA,
                        s?.StudentSkills
                            .Select(sk => sk.Skill?.Name ?? string.Empty)
                            .Where(n => n.Length > 0)
                            .ToList()
                            .AsReadOnly()
                        ?? new List<string>().AsReadOnly()
                    );
                })
                .ToList()
                .AsReadOnly();

            var technologies = project.ProjectTechnologies
                .Select(pt => pt.Technology?.Name ?? string.Empty)
                .Where(n => n.Length > 0)
                .ToList()
                .AsReadOnly();

            var feedbackHistory = project.Feedbacks
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new ProjectFeedbackItemDto(f.Id, f.Content, f.CreatedAt, f.IsRead))
                .ToList()
                .AsReadOnly();

            var hasReport = await _unitOfWork.Repository<OriginalityReport>()
                .AnyAsync(r => r.ProjectId == projectId);

            return new ProfessorProjectDetailDto(
                project.Id, project.Title, project.Abstract,
                project.Description, project.ProblemStatement,
                project.ProposedSolution, project.Objectives,
                project.Status.ToString(),
                ToProgressPercent(project.Status),
                project.OriginalityScore,
                project.Team!.Name,
                project.Domain?.Name ?? "Unknown",
                project.AcademicYear?.Name ?? "Unknown",
                memberDtos,
                technologies,
                project.CreatedAt, project.SubmittedAt,
                feedbackHistory,
                hasReport
            );
        }

        public async Task RequestRevisionAsync(int professorId, int projectId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("A revision reason is required.");

            var project = await _unitOfWork.Repository<Project>().GetByIdAsync(projectId)
                ?? throw new KeyNotFoundException("Project not found.");

            if (!project.TeamId.HasValue)
                throw new InvalidOperationException("Project is not associated with a team.");

            var team = await _unitOfWork.Repository<Team>().GetByIdAsync(project.TeamId.Value)
                ?? throw new KeyNotFoundException("Team not found.");

            if (team.ProfessorId != professorId)
                throw new UnauthorizedAccessException(
                    "You are not authorized to request revisions on this project.");

            if (project.Status != ProjectStatus.UnderReview)
                throw new InvalidOperationException(
                    "Revision can only be requested for projects with 'UnderReview' status.");

            // Return project to Draft so the team can edit and resubmit
            project.Status = ProjectStatus.Draft;
            project.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Project>().Update(project);

            // Store revision reason as a Feedback record for traceability.
            // The [REVISION] prefix lets students visually distinguish revision notes
            // from general feedback in the UI.
            var feedback = new Feedback
            {
                ProfessorId = professorId,
                ProjectId = projectId,
                Content = $"[REVISION REQUESTED] {reason}",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            await _unitOfWork.Repository<Feedback>().AddAsync(feedback);
            await _unitOfWork.CompleteAsync();

            await NotifyTeamMembersAsync(
                project.TeamId.Value,
                "Revision Requested",
                $"Your supervisor has requested revisions on '{project.Title}'. " +
                $"Review the feedback and resubmit.",
                NotificationType.Warning,
                project.Id);

            // Log activity
            var professor = await _unitOfWork.Repository<Professor>().GetByIdAsync(professorId);
            var log = new ProjectActivityLog
            {
                ProjectId = projectId,
                Type = "warning",
                Message = $"Revision requested: {reason}",
                ActorName = professor?.FullName ?? "Supervisor",
                IconName = "FileText",
                ColorClass = "text-amber-500",
                BgClass = "bg-amber-500/10"
            };
            await _unitOfWork.Repository<ProjectActivityLog>().AddAsync(log);
            await _unitOfWork.CompleteAsync();

            await _notificationService.SendProjectActivityLogAsync(projectId, log.Type, log.Message, log.ActorName, log.IconName, log.ColorClass, log.BgClass);
        }

        public async Task<ProfessorDashboardDto> GetDashboardAsync(int professorId)
        {
            _ = await _unitOfWork.Repository<Professor>().GetByIdAsync(professorId)
                ?? throw new KeyNotFoundException("Professor not found.");

            // Single DB round-trip for all project-level stats
            var stats = await _unitOfWork.Repository<Project>()
                .GetQueryable()
                .AsNoTracking()
                .Where(p => p.Team != null && p.Team.ProfessorId == professorId)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    PendingReview = g.Count(p => p.Status == ProjectStatus.UnderReview),
                    Active = g.Count(p => p.Status == ProjectStatus.In_Progress),
                    Approved = g.Count(p => p.Status == ProjectStatus.Approved),
                    Rejected = g.Count(p => p.Status == ProjectStatus.Rejected),
                    AvgScore = g.Where(p => p.OriginalityScore != null)
                                       .Select(p => (decimal?)p.OriginalityScore)
                                       .Average()
                })
                .FirstOrDefaultAsync();

            var totalTeams = await _unitOfWork.Repository<Team>()
                .CountAsync(t => t.ProfessorId == professorId &&
                                 (t.Project == null || t.Project.Status != ProjectStatus.Completed));

            // Reuse the fixed GetSupervisedTeamsAsync for the recent-teams widget, only approved/active
            var recentTeams = (await GetSupervisedTeamsAsync(professorId))
                .Where(t => t.ProjectStatus == ProjectStatus.In_Progress.ToString() || 
                            t.ProjectStatus == ProjectStatus.Approved.ToString() ||
                            t.ProjectStatus == ProjectStatus.Completed.ToString())
                .OrderByDescending(t => t.SubmittedAt ?? DateTime.MinValue)
                .Take(5)
                .ToList()
                .AsReadOnly();

            return new ProfessorDashboardDto(
                totalTeams,
                stats?.PendingReview ?? 0,
                stats?.Active ?? 0,
                stats?.Approved ?? 0,
                stats?.Rejected ?? 0,
                stats?.AvgScore.HasValue == true
                    ? Math.Round(stats!.AvgScore!.Value, 2)
                    : null,
                recentTeams
            );
        }

    }
}