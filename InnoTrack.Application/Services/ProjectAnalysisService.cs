using InnoTrack.Application.DTOs.AI;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Entities.Enums;
using InnoTrack.Domain.Interfaces;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<ProjectAnalysisService> _logger;

        public ProjectAnalysisService(IUnitOfWork unitOfWork, IPythonAiClient aiClient, INotificationService notificationService, ILogger<ProjectAnalysisService> logger)
        {
            _unitOfWork = unitOfWork;
            _aiClient = aiClient;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ProcessProjectAiReportAsync(int projectId)
        {
            var project = await _unitOfWork.Repository<Project>().GetByIdAsync(projectId);
            if (project is null)
            {
                _logger.LogWarning("Project {ProjectId} not found during AI analysis.", projectId);
                return;
            }

            var request = new PythonAiRequestDto(project.Id, project.Title, project.Abstract, project.Description);
            var aiResponse = await _aiClient.AnalyzeProjectAsync(request);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var vector = new VectorEmbedding
                {
                    ProjectId = project.Id,
                    ModelName = "SBERT",
                    VectorData = aiResponse.VectorData,
                    GeneratedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<VectorEmbedding>().AddAsync(vector);

                var report = new OriginalityReport
                {
                    ProjectId = project.Id,
                    OverallScore = aiResponse.OriginalityScore,
                    Summary = aiResponse.Summary,
                    GeneratedAt = DateTime.UtcNow
                };
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

                project.OriginalityScore = aiResponse.OriginalityScore;
                project.UpdatedAt = DateTime.UtcNow;

                var (status, notifTitle, notifMessage, notifType) = aiResponse.OriginalityScore switch
                {
                    < 60 => (ProjectStatus.Rejected, "Project Rejected",
                              $"Originality score {aiResponse.OriginalityScore}% is too low.",
                              NotificationType.Error),
                    <= 70 => (ProjectStatus.Submitted, "AI Analysis — Review Required",
                              $"Originality score {aiResponse.OriginalityScore}%. Professor approval required.",
                              NotificationType.Warning),
                    _ => (ProjectStatus.Submitted, "AI Analysis Passed",
                              $"Originality score {aiResponse.OriginalityScore}%. Awaiting professor review.",
                              NotificationType.Success)
                };

                project.Status = status;
                _unitOfWork.Repository<Project>().Update(project);

                await _unitOfWork.CommitTransactionAsync();

                try
                {
                    var teamLeader = await _unitOfWork.Repository<TeamMember>()
                        .FindAsync(tm => tm.TeamId == project.TeamId && tm.Role == TeamMemberRole.Leader);

                    if (teamLeader != null)
                    {
                        await _notificationService.SendNotificationAsync(
                            teamLeader.StudentId, notifTitle, notifMessage, notifType);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send notification for project {ProjectId}", project.Id);
                }
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
