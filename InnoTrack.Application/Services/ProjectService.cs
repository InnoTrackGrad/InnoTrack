using Hangfire;
using InnoTrack.Application.DTOs.Projects;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Entities.Enums;
using InnoTrack.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InnoTrack.Application.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProjectService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task VerifyProjectForSubmissionAsync(int projectId, int userId, SubmitProjectRequestDto dto)
        {
            var supervisorId = dto.SupervisorId;
            var draft = await _unitOfWork.Repository<ProjectDraft>()
                .GetQueryable()
                .Include(d => d.DraftTechnologies)
                .FirstOrDefaultAsync(d => d.Id == projectId);
            if (draft == null)
                throw new KeyNotFoundException("Draft not found.");

            var leaderRecord = await _unitOfWork.Repository<TeamMember>()
                .FindAsync(tm => tm.TeamId == draft.TeamId && tm.StudentId == userId);

            if (leaderRecord == null || leaderRecord.Role != TeamMemberRole.Leader)
                throw new UnauthorizedAccessException("Only the team leader can submit the project.");

            var existingProject = await _unitOfWork.Repository<Project>()
                .FindAsync(p => p.TeamId == draft.TeamId);
            if (existingProject != null)
                throw new InvalidOperationException("Your team already has an active or submitted project.");

            var supervisor = await _unitOfWork.Repository<Professor>()
                .GetQueryable()
                .Include(p => p.SupervisedTeams)
                .FirstOrDefaultAsync(p => p.Id == supervisorId);

            if (supervisor == null)
                throw new KeyNotFoundException("Supervisor not found.");

            if (supervisor.SupervisedTeams.Count >= supervisor.MaxTeamLoad)
                throw new InvalidOperationException($"Dr. {supervisor.FullName} has reached their maximum capacity of teams.");

            var team = await _unitOfWork.Repository<Team>().GetByIdAsync(draft.TeamId);
            if (team == null)
                throw new KeyNotFoundException("Team not found.");

            var activeAcademicYear = await _unitOfWork.Repository<AcademicYear>()
                .FindAsync(y => y.IsActive);
            if (activeAcademicYear == null)
                throw new InvalidOperationException("Academic year configuration is missing. Please contact administration.");

            await _unitOfWork.BeginTransactionAsync();
            Project project;
            try
            {
                team.ProfessorId = supervisorId;
                _unitOfWork.Repository<Team>().Update(team);

                project = new Project
                {
                    Title = draft.Title,
                    Abstract = draft.Abstract,
                    Description = draft.Description,
                    Status = ProjectStatus.UnderReview,
                    OriginalityScore = draft.OriginalityScore,
                    Year = draft.Year,
                    StudentNames = draft.StudentNames,
                    CreatedAt = DateTime.UtcNow,
                    SubmittedAt = DateTime.UtcNow,
                    TeamId = draft.TeamId,
                    DomainId = draft.DomainId,
                    AcademicYearId = activeAcademicYear.Id,
                    ProblemStatement = draft.ProblemStatement,
                    ProposedSolution = draft.ProposedSolution,
                    Objectives = draft.Objectives,
                    ProposalDepartment = dto.Department,
                    ProposalTeamMembers = dto.TeamMembers,
                    ProposalMessage = dto.Message,
                };

                await _unitOfWork.Repository<Project>().AddAsync(project);
                await _unitOfWork.CompleteAsync();

                foreach (var draftTech in draft.DraftTechnologies)
                {
                    await _unitOfWork.Repository<ProjectTechnology>().AddAsync(new ProjectTechnology
                    {
                        ProjectId = project.Id,
                        TechnologyId = draftTech.TechnologyId,
                    });
                }

                _unitOfWork.Repository<ProjectDraft>().Delete(draft);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            BackgroundJob.Enqueue<IProjectAnalysisService>(aiService => aiService.ProcessProjectAiReportAsync(project.Id));
        }
    }
}