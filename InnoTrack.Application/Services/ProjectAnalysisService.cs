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
using System.Text.Json;

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
                var request = new PythonAiRequestDto(
                    title: project.Title,
                    description: project.Description,
                    abstractText: project.Abstract
                );
                aiResponse = await _aiClient.AnalyzeProjectAsync(request);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error processing AI for project {ProjectId}", projectId);

                if (project.Status == ProjectStatus.UnderReview)
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
                decimal overallScore = aiResponse.TopSimilarProjects.FirstOrDefault()?.FinalOriginalityScore ?? 100m;
                
                string generatedSummary = aiResponse.ExtractedFeatures.Any()
                    ? "Features: " + string.Join(", ", aiResponse.ExtractedFeatures)
                    : "No summary generated.";

                var vector = new VectorEmbedding
                {
                    ProjectId = project.Id,
                    ModelName = "SBERT",
                    VectorData = "Data omitted by AI",
                    GeneratedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<VectorEmbedding>().AddAsync(vector);

                var report = new OriginalityReport
                {
                    ProjectId = project.Id,
                    OverallScore = overallScore,
                    Summary = generatedSummary,
                    GeneratedAt = DateTime.UtcNow
                };

                foreach (var sp in aiResponse.TopSimilarProjects)
                {
                    var referencedProject = await _unitOfWork.Repository<Project>()
                        .GetQueryable()
                        .FirstOrDefaultAsync(p => p.Title == sp.ProjectTitle);

                    var matchedFeatures = sp.MatchedFeatures ?? new List<JsonElement>();

                    report.SimilarProjects.Add(new SimilarProject
                    {
                        ReferencedProjectId = referencedProject?.Id ?? 0,
                        SimilarityPercentage = sp.SimilarityScore,
                        MatchReason = "Matched: " + string.Join(", ", matchedFeatures.Select(f => f.ToString()))
                    });
                }
                await _unitOfWork.Repository<OriginalityReport>().AddAsync(report);

                project.OriginalityScore = overallScore;
                project.UpdatedAt = DateTime.UtcNow;

                var (status, notifTitle, notifMessage, notifType) = overallScore switch
                {
                    var score when score < _thresholds.AutoRejectBelow
                                    => (ProjectStatus.Rejected, "Project Rejected", $"Originality score {score}% is too low.", NotificationType.Error),

                    var score when score <= _thresholds.RequireManualReviewBelow
                                    => (ProjectStatus.UnderReview, "AI Analysis — Review Required", $"Originality score {score}%. Professor approval required.", NotificationType.Warning),

                    _ => (ProjectStatus.UnderReview, "AI Analysis Passed", $"Originality score {overallScore}%. Awaiting professor review.", NotificationType.Success)
                };

                var currentProjectState = await _unitOfWork.Repository<Project>().GetByIdAsync(project.Id);
                if (currentProjectState != null && currentProjectState.Status == ProjectStatus.Draft)
                {
                    currentProjectState.OriginalityScore = overallScore;
                    _unitOfWork.Repository<Project>().Update(currentProjectState);
                    await _unitOfWork.CompleteAsync();
                    await _unitOfWork.CommitTransactionAsync();
                    return;
                }

                project.Status = status;
                _unitOfWork.Repository<Project>().Update(project);

                await _unitOfWork.CompleteAsync();
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
                    .ThenInclude(sp => sp.ReferencedProject)
                .FirstOrDefaultAsync(r => r.ProjectId == projectId);

            if (report == null)
                throw new KeyNotFoundException("AI report not generated yet for this project.");

            return new OriginalityReportDto(
                report.ProjectId, report.OverallScore, report.Summary, report.GeneratedAt,
                report.SimilarProjects
                    .Select(sp => new SimilarProjectResultDto(
                            sp.ReferencedProjectId,
                            sp.ReferencedProject?.Title ?? $"Project #{sp.ReferencedProjectId}",
                            sp.SimilarityPercentage, 
                            sp.MatchReason
                        ))
                    .ToList()
                    .AsReadOnly()
    );
        }
    }
}
