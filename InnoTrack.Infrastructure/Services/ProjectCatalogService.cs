using InnoTrack.Application.Common;
using InnoTrack.Application.DTOs.AI;
using InnoTrack.Application.DTOs.Projects;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Entities.Enums;
using InnoTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnoTrack.Infrastructure.Services
{
    public class ProjectCatalogService : IProjectCatalogService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPythonAiClient _aiClient;

        public ProjectCatalogService(ApplicationDbContext context, IPythonAiClient aiClient)
        {
            _context = context;
            _aiClient = aiClient;
        }

        public async Task<PagedResult<ProjectCatalogItemDto>> GetProjectsAsync(
            int? year, string? status, string? search, int pageNumber, int pageSize)
        {
            var query = _context.Projects
                .AsNoTracking()
                .Include(p => p.Domain)
                .Include(p => p.Team).ThenInclude(t => t.Supervisor)
                .Include(p => p.Team).ThenInclude(t => t.Members).ThenInclude(m => m.Student)
                .Include(p => p.ProjectTechnologies).ThenInclude(pt => pt.Technology)
                .AsQueryable();

            if (year.HasValue)
                query = query.Where(p => p.CreatedAt.Year == year.Value);

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.Equals("completed", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(p => p.Status == ProjectStatus.Completed);
                else if (status.Equals("in-progress", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(p => p.Status != ProjectStatus.Completed
                                             && p.Status != ProjectStatus.Rejected);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.Title.Contains(search) ||
                    p.Domain.Name.Contains(search) ||
                    p.ProjectTechnologies.Any(pt => pt.Technology.Name.Contains(search)));
            }

            var totalCount = await query.CountAsync();

            var data = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProjectCatalogItemDto(
                    p.Id,
                    p.Title,
                    p.Domain.Name,
                    MapStatus(p.Status),
                    p.CreatedAt.Year,
                    p.Team.Supervisor != null ? p.Team.Supervisor.FullName : null,
                    p.Team.Members.Select(m => m.Student.FullName).ToList(),
                    p.ProjectTechnologies.Select(pt => pt.Technology.Name).ToList(),
                    p.OriginalityScore
                ))
                .ToListAsync();

            return new PagedResult<ProjectCatalogItemDto>(data.AsReadOnly(), totalCount, pageNumber, pageSize);
        }

        public async Task<ProjectCatalogDetailDto> GetProjectByIdAsync(int projectId)
        {
            var project = await _context.Projects
                .AsNoTracking()
                .Include(p => p.Team).ThenInclude(t => t.Supervisor)
                .Include(p => p.Team).ThenInclude(t => t.Members)
                    .ThenInclude(m => m.Student).ThenInclude(s => s.Department)
                .Include(p => p.ProjectTechnologies).ThenInclude(pt => pt.Technology)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                throw new KeyNotFoundException("Project not found.");

            var students = project.Team.Members.Select(m => new StudentInProjectDto(
                m.Student.FullName,
                m.Role.ToString(),
                m.Student.Department?.Name ?? string.Empty
            )).ToList();

            return new ProjectCatalogDetailDto(
                project.Id, project.Title, project.Domain.Name, MapStatus(project.Status),
                project.CreatedAt.Year, project.Team.Supervisor?.FullName,
                project.ProjectTechnologies.Select(pt => pt.Technology.Name).ToList().AsReadOnly(),
                project.OriginalityScore, project.SubmittedAt, project.UpdatedAt,
                project.Description, project.Abstract, students.AsReadOnly()
            );
        }

        public async Task<MyProjectResponseDto?> GetMyProjectAsync(int userId)
        {
            var teamMember = await _context.TeamMembers
                .AsNoTracking()
                .Include(tm => tm.Team).ThenInclude(t => t.Project)
                     .ThenInclude(p => p!.Domain)
                .Include(tm => tm.Team).ThenInclude(t => t.Project)
                     .ThenInclude(p => p!.ProjectTechnologies).ThenInclude(pt => pt.Technology)
                .Include(tm => tm.Team).ThenInclude(t => t.Members).ThenInclude(m => m.Student)
                .FirstOrDefaultAsync(tm => tm.StudentId == userId);

            var project = teamMember?.Team?.Project;
            if (project == null) return null;

            var members = teamMember!.Team.Members
                .Select(m => new TeamMemberSummaryDto(m.Student.FullName, m.Role.ToString()))
                .ToList();

            var technologies = project.ProjectTechnologies
                .Select(pt => pt.Technology.Name)
                .ToList();

            return new MyProjectResponseDto(
                ProjectId: project.Id,
                Title: project.Title,
                Status: project.Status.ToString(),
                DomainName: project.Domain?.Name,
                OriginalityScore: project.OriginalityScore,
                JoinCode: teamMember.Team.JoinCode,
                Technologies: technologies.AsReadOnly(),
                Members: members.AsReadOnly(),
                CreatedAt: project.CreatedAt,
                SubmittedAt: project.SubmittedAt
            );
        }

        public async Task<IReadOnlyList<SupervisorDto>> GetSupervisorsAsync()
        {
            var supervisors = await _context.Professors
                .AsNoTracking()
                .Include(p => p.Department)
                .Include(p => p.SupervisedTeams)
                .Select(p => new SupervisorDto(
                    p.Id,
                    p.FullName,
                    p.Department.Name,
                    p.Specialization,
                    p.Email,
                    p.SupervisedTeams.Count
                ))
                .ToListAsync();

            return supervisors.AsReadOnly();
        }

        public async Task<SaveDraftResponseDto> SaveDraftAsync(int userId, SaveProjectDraftDto dto)
        {
            var leaderRecord = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.StudentId == userId && tm.Role == TeamMemberRole.Leader);
            if (leaderRecord == null)
                throw new UnauthorizedAccessException("Only a team leader can create a project draft.");

            var existingProject = await _context.Projects
                .FirstOrDefaultAsync(p => p.TeamId == leaderRecord.TeamId);
            if (existingProject != null)
                throw new InvalidOperationException("Your team already has a project.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var project = new Project
                {
                    Title = dto.Title,
                    Abstract = dto.Abstract,
                    Description = dto.Description,
                    DomainId = dto.DomainId,
                    TeamId = leaderRecord.TeamId,
                    Status = ProjectStatus.Draft
                };
                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                foreach (var techId in dto.TechnologyIds)
                {
                    _context.ProjectTechnologies.Add(
                        new ProjectTechnology { ProjectId = project.Id, TechnologyId = techId });
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new SaveDraftResponseDto(project.Id, project.Title, project.CreatedAt);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<SaveDraftResponseDto> UpdateDraftAsync(int draftId, int userId, SaveProjectDraftDto dto)
        {
            var project = await _context.Projects.FindAsync(draftId);
            if (project == null) throw new KeyNotFoundException("Draft not found.");

            if (project.Status != ProjectStatus.Draft)
                throw new InvalidOperationException("Only Draft projects can be updated.");

            var leaderRecord = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == project.TeamId
                                            && tm.StudentId == userId
                                            && tm.Role == TeamMemberRole.Leader);
            if (leaderRecord == null)
                throw new UnauthorizedAccessException("Only the team leader can update this draft.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                project.Title = dto.Title; project.Abstract = dto.Abstract;
                project.Description = dto.Description; project.DomainId = dto.DomainId;
                project.UpdatedAt = DateTime.UtcNow;

                var existingTechs = _context.ProjectTechnologies.Where(pt => pt.ProjectId == draftId);
                _context.ProjectTechnologies.RemoveRange(existingTechs);

                foreach (var techId in dto.TechnologyIds)
                {
                    _context.ProjectTechnologies.Add(
                        new ProjectTechnology { ProjectId = draftId, TechnologyId = techId });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new SaveDraftResponseDto(project.Id, project.Title, project.UpdatedAt.Value);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteDraftAsync(int draftId, int userId)
        {
            var project = await _context.Projects.FindAsync(draftId);
            if (project == null)
                throw new KeyNotFoundException("Draft not found.");

            if (project.Status != ProjectStatus.Draft)
                throw new InvalidOperationException("Only Draft projects can be deleted.");

            var leaderRecord = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == project.TeamId
                                          && tm.StudentId == userId
                                          && tm.Role == TeamMemberRole.Leader);
            if (leaderRecord == null)
                throw new UnauthorizedAccessException("Only the team leader can delete this draft.");

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateProjectDetailsAsync(int projectId, int userId, UpdateProjectDetailsDto dto)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) throw new KeyNotFoundException("Project not found.");

            var leaderRecord = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == project.TeamId && tm.StudentId == userId && tm.Role == TeamMemberRole.Leader);
            if (leaderRecord == null) throw new UnauthorizedAccessException("Only the team leader can update project details.");

            if (dto.Title != null) project.Title = dto.Title;
            if (dto.Description != null) project.Description = dto.Description;
            project.UpdatedAt = DateTime.UtcNow;

            if (dto.TechnologyIds != null)
            {
                var existing = _context.ProjectTechnologies.Where(pt => pt.ProjectId == projectId);
                _context.ProjectTechnologies.RemoveRange(existing);
                foreach (var techId in dto.TechnologyIds)
                {
                    _context.ProjectTechnologies.Add(new ProjectTechnology { ProjectId = projectId, TechnologyId = techId });
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task RecallSubmissionAsync(int projectId, int userId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) throw new KeyNotFoundException("Project not found.");

            if (project.Status != ProjectStatus.Submitted)
                throw new InvalidOperationException("Only submitted projects can be recalled.");

            var leaderRecord = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == project.TeamId && tm.StudentId == userId && tm.Role == TeamMemberRole.Leader);
            if (leaderRecord == null) throw new UnauthorizedAccessException("Only the team leader can recall a submission.");

            project.Status = ProjectStatus.Draft;
            project.SubmittedAt = null;
            project.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<SimilarityCheckResponseDto> RunSimilarityCheckAsync(SimilarityCheckRequestDto dto)
        {
            var aiRequest = new PythonAiRequestDto(0, dto.Title, dto.Abstract, dto.Description);
            var aiResponse = await _aiClient.AnalyzeProjectAsync(aiRequest);

            var similarProjects = new List<SimilarProjectResultDto>();
            foreach (var sp in aiResponse.SimilarProjects)
            {
                var referencedProject = await _context.Projects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == sp.ReferencedProjectId);

                similarProjects.Add(new SimilarProjectResultDto(
                    sp.ReferencedProjectId,
                    referencedProject?.Title ?? "Unknown Project",
                    sp.SimilarityPercentage,
                    sp.MatchReason
                ));
            }

            return new SimilarityCheckResponseDto(aiResponse.OriginalityScore, similarProjects.AsReadOnly());
        }

        private static string MapStatus(ProjectStatus status) => status switch
        {
            ProjectStatus.Completed => "completed",
            ProjectStatus.Rejected => "rejected",
            _ => "in-progress"
        };

    }
}