using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Entities.Enums;
using InnoTrack.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InnoTrack.Infrastructure.BackgroundJobs
{
    public class AiProcessingBackgroundService : BackgroundService
    {
        private readonly IProjectAnalysisQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AiProcessingBackgroundService> _logger;

        public AiProcessingBackgroundService(IProjectAnalysisQueue queue, IServiceScopeFactory scopeFactory, ILogger<AiProcessingBackgroundService> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var projectId in _queue.DequeueAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var analysisService = scope.ServiceProvider.GetRequiredService<IProjectAnalysisService>();
                    await analysisService.ProcessProjectAiReportAsync(projectId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing AI for project {ProjectId}", projectId);

                    try
                    {
                        using var errorScope = _scopeFactory.CreateScope();
                        var uow = errorScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                        var project = await uow.Repository<Project>().GetByIdAsync(projectId);
                        if (project is not null && project.Status == ProjectStatus.Processing)
                        {
                            project.Status = ProjectStatus.Draft;
                            uow.Repository<Project>().Update(project);
                            await uow.CompleteAsync();

                            var notificationSvc = errorScope.ServiceProvider.GetRequiredService<INotificationService>();
                            var leader = await uow.Repository<TeamMember>()
                                .FindAsync(tm => tm.TeamId == project.TeamId && tm.Role == TeamMemberRole.Leader);

                            if (leader != null)
                            {
                                await notificationSvc.SendNotificationAsync(leader.StudentId,
                                    "Analysis Failed",
                                    "The AI analysis failed due to a server error. Please resubmit your project.",
                                    NotificationType.Error);
                            }

                        }
                    }
                    catch (Exception innerEx)
                    {
                        _logger.LogError(innerEx, "Failed to revert project {ProjectId} after AI error", projectId);
                    }

                }
            }
        }
    }
}
