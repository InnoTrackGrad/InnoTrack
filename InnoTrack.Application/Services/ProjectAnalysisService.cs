using InnoTrack.Application.DTOs.AI;
using InnoTrack.Application.DTOs.Projects;
using InnoTrack.Application.Interfaces;
using InnoTrack.Application.Settings;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Entities.Enums;
using InnoTrack.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;

namespace InnoTrack.Application.Services
{
    public class ProjectAnalysisService : IProjectAnalysisService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPythonAiClient _aiClient;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ProjectAnalysisService> _logger;
        private readonly OriginalityThresholds _thresholds;

        public ProjectAnalysisService(IUnitOfWork unitOfWork, IPythonAiClient aiClient, INotificationService notificationService, ILogger<ProjectAnalysisService> logger, IOptions<OriginalityThresholds> options)
        {
            _unitOfWork = unitOfWork;
            _aiClient = aiClient;
            _notificationService = notificationService;
            _logger = logger;
            _thresholds = options.Value;
        }

        public async Task ProcessProjectAiReportAsync(int projectId)
        {
            var project = await _unitOfWork.Repository<Project>().GetByIdAsync(projectId);
            if (project is null)
            {
                _logger.LogWarning("Project {ProjectId} not found during AI analysis.", projectId);
                return;
            }

            PythonAiResponseDto aiResponse;

            try
            {
                var request = new PythonAiRequestDto(project.Id, project.Title, project.Abstract, project.Description);
                aiResponse = await _aiClient.AnalyzeProjectAsync(request);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error processing AI for project {ProjectId}", projectId);

                if (project.Status == ProjectStatus.Processing)
                {
                    project.Status = ProjectStatus.Draft;
                    _unitOfWork.Repository<Project>().Update(project);
                    await _unitOfWork.CompleteAsync();

                    var leader = await _unitOfWork.Repository<TeamMember>()
                        .FindAsync(tm => tm.TeamId == project.TeamId && tm.Role == TeamMemberRole.Leader);

                    if (leader != null)
                    {
                        await _notificationService.SendNotificationAsync(leader.StudentId,
                            "Analysis Failed",
                            "The AI analysis failed due to a server error. Please resubmit your project.",
                            NotificationType.Error);
                    }
                }

                throw;
            }

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
                    var score when score < _thresholds.AutoRejectBelow
                                    => (ProjectStatus.Rejected, "Project Rejected", $"Originality score {score}% is too low.", NotificationType.Error),

                    var score when score <= _thresholds.RequireManualReviewBelow
                                    => (ProjectStatus.Submitted, "AI Analysis — Review Required", $"Originality score {score}%. Professor approval required.", NotificationType.Warning),

                    _ => (ProjectStatus.Submitted, "AI Analysis Passed", $"Originality score {aiResponse.OriginalityScore}%. Awaiting professor review.", NotificationType.Success)
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

        public async Task<OriginalityReportDto> GetOriginalityReportAsync(int projectId)
        {
            var report = await _unitOfWork.Repository<OriginalityReport>()
                .GetQueryable()
                .Include(r => r.SimilarProjects)
                .FirstOrDefaultAsync(r => r.ProjectId == projectId);

            if (report == null) throw new KeyNotFoundException("AI report not generated yet.");

            return new OriginalityReportDto(
                report.ProjectId, report.OverallScore, report.Summary, report.GeneratedAt,
                report.SimilarProjects.Select(sp => new SimilarProjectResultDto(sp.ReferencedProjectId, "", sp.SimilarityPercentage, sp.MatchReason)).ToList().AsReadOnly()
    );
        }
    }
}
