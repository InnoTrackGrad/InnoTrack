using AutoMapper;
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
            var project = await _unitOfWork.Repository<Project>().GetByIdAsync(projectId);
            if (project == null)
                throw new KeyNotFoundException("Project not found.");

            if (project.Status != ProjectStatus.Draft)
                throw new InvalidOperationException("Only projects in Draft status can be submitted.");

            var leaderRecord = await _unitOfWork.Repository<TeamMember>()
                .FindAsync(tm => tm.TeamId == project.TeamId && tm.StudentId == userId);

            if (leaderRecord == null || leaderRecord.Role != TeamMemberRole.Leader)
                throw new UnauthorizedAccessException("Only the team leader can submit the project.");

            var supervisor = await _unitOfWork.Repository<Professor>()
                .GetQueryable()
                .Include(p => p.SupervisedTeams)
                .FirstOrDefaultAsync(p => p.Id == supervisorId);

            if (supervisor == null)
                throw new KeyNotFoundException("Supervisor not found.");

            if (supervisor.SupervisedTeams.Count >= supervisor.MaxTeamLoad)
                throw new InvalidOperationException($"Dr. {supervisor.FullName} has reached their maximum capacity of teams.");

            var team = await _unitOfWork.Repository<Team>().GetByIdAsync(project.TeamId);
            if (team == null)
                throw new KeyNotFoundException("Team not found.");

            team.ProfessorId = supervisorId;
            _unitOfWork.Repository<Team>().Update(team);

            project.Status = ProjectStatus.UnderReview;
            project.SubmittedAt = DateTime.UtcNow;

            project.ProposalDepartment = dto.Department;
            project.ProposalTeamMembers = dto.TeamMembers;
            project.ProposalMessage = dto.Message;

            _unitOfWork.Repository<Project>().Update(project);
            await _unitOfWork.CompleteAsync();
            BackgroundJob.Enqueue<IProjectAnalysisService>(aiService => aiService.ProcessProjectAiReportAsync(projectId));
        }
    }
}
