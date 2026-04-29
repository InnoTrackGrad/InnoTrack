using InnoTrack.Application.Interfaces;
using InnoTrack.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Infrastructure.BackgroundJobs
{
    public class AiProcessingBackgroundService : BackgroundService
    {
        private readonly ProjectAnalysisQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AiProcessingBackgroundService> _logger;

        public AiProcessingBackgroundService(ProjectAnalysisQueue queue, IServiceScopeFactory scopeFactory, ILogger<AiProcessingBackgroundService> logger)
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
                    _logger.LogError(ex, $"Error processing AI for project {projectId}");
                }
            }
        }
    }
}
