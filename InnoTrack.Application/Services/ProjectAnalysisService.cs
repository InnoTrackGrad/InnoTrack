using InnoTrack.Application.DTOs.AI;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Entities.Enums;
using InnoTrack.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.Services
{
    public class ProjectAnalysisService : IProjectAnalysisService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPythonAiClient _aiClient;
        private readonly INotificationService _notificationService;

        public ProjectAnalysisService(IUnitOfWork unitOfWork, IPythonAiClient aiClient, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _aiClient = aiClient;
            _notificationService = notificationService;
        }

        public async Task ProcessProjectAiReportAsync(int projectId)
        {
            var project = await _unitOfWork.Repository<Project>().GetByIdAsync(projectId);
            if (project == null) return;

            var request = new PythonAiRequestDto(project.Id, project.Title, project.Abstract, project.Description);
            var aiResponse = await _aiClient.AnalyzeProjectAsync(request);

            var vector = new VectorEmbedding { ProjectId = project.Id, ModelName = "SBERT", VectorData = aiResponse.VectorData };
            await _unitOfWork.Repository<VectorEmbedding>().AddAsync(vector);

            var report = new OriginalityReport { ProjectId = project.Id, OverallScore = aiResponse.OriginalityScore, Summary = aiResponse.Summary };
            foreach (var similar in aiResponse.SimilarProjects)
            {
                report.SimilarProjects.Add(new SimilarProject
                {
                    ReferencedProjectId = similar.ReferencedProjectId,
                    SimilarityPercentage = similar.SimilarityPercentage,
                    MatchReason = similar.MatchReason
                });
            }
            await _unitOfWork.Repository<OriginalityReport>().AddAsync(report);

            var teamLeader = await _unitOfWork.Repository<TeamMember>()
                .FindAsync(tm => tm.TeamId == project.TeamId && tm.Role == TeamMemberRole.Leader);
            
            if(teamLeader != null)
            {
                project.OriginalityScore = aiResponse.OriginalityScore;

                if (aiResponse.OriginalityScore < 60)
                {
                    project.Status = ProjectStatus.Rejected;
                    await _notificationService.SendNotificationAsync(
                        teamLeader.StudentId, 
                        "Project Rejected", 
                        $"Originality score too low: {project.OriginalityScore}%",
                        NotificationType.Error);
                }
                else
                {
                    project.Status = ProjectStatus.Submitted;
                    await _notificationService.SendNotificationAsync(
                        teamLeader.StudentId,
                        "AI Analysis Passed",
                        $"Originality score: {project.OriginalityScore}%. Waiting for professor review.", 
                        NotificationType.Success);
                }
            }
            
            _unitOfWork.Repository<Project>().Update(project);
            await _unitOfWork.CompleteAsync();
        }
    }
}
