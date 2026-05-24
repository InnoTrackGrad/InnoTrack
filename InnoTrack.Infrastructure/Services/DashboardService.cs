using InnoTrack.Application.DTOs.Dashboard;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities.Enums;
using InnoTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnoTrack.Infrastructure.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context) => _context = context;

        public async Task<GlobalDashboardStatsDto> GetGlobalDashboardStatsAsync()
        {
            var activeYear = await _context.AcademicYears.FirstOrDefaultAsync(y => y.IsActive);
            int activeYearId = activeYear?.Id ?? 0;

            var totalProjects = await _context.Projects.CountAsync();

            var completedThisYear = await _context.Projects.CountAsync(
                    p => p.Status == ProjectStatus.Completed && p.AcademicYearId == activeYearId);

            var inProgress = await _context.Projects.CountAsync(p =>
                p.Status == ProjectStatus.Draft
                || p.Status == ProjectStatus.Processing
                || p.Status == ProjectStatus.Submitted);

            var topTechnologies = await _context.ProjectTechnologies
                    .GroupBy(pt => pt.Technology.Name)
                    .Select(g => new { Name = g.Key, Count = g.Count() })
                    .OrderByDescending(t => t.Count)
                    .Take(5)
                    .ToListAsync();

            var topTechnologiesDto = topTechnologies
                    .Select(t => new TechStatDto(t.Name, t.Count))
                    .ToList();

            return new GlobalDashboardStatsDto(
                totalProjects,
                completedThisYear,
                inProgress,
                topTechnologiesDto.AsReadOnly()
            );
        }

        public async Task<IReadOnlyList<PopularProjectDto>> GetPopularProjectsAsync(int limit)
        {
            var projects = await _context.Projects
                .AsNoTracking()
                .Where(p => p.Status == ProjectStatus.Completed && p.OriginalityScore.HasValue)
                .OrderByDescending(p => p.OriginalityScore)
                .Take(limit)
                .Select(p => new PopularProjectDto(
                    p.Id,
                    p.Title,
                    p.Domain.Name,
                    p.OriginalityScore,
                    p.CreatedAt.Year,
                    p.Team.Supervisor != null ? p.Team.Supervisor.FullName : null,
                    p.Team.Members.Select(m => m.Student.FullName).ToList()
                    ))
                .ToListAsync();

            return projects.AsReadOnly();
        }

        public async Task<IReadOnlyList<TrendingTechnologyDto>> GetTrendingTechnologiesAsync()
        {
            var currentYear = DateTime.UtcNow.Year;

            var trending = await _context.ProjectTechnologies
                   .AsNoTracking()
                   .Where(pt => pt.Project.CreatedAt.Year == currentYear)
                   .GroupBy(pt => pt.Technology.Name)
                   .Select(g => new { Name = g.Key, Count = g.Count() })
                   .OrderByDescending(t => t.Count)
                   .Take(10)
                   .ToListAsync();

            var trendingDto = trending
                    .Select(t => new TrendingTechnologyDto(t.Name, t.Count))
                    .ToList();

            return trendingDto.AsReadOnly();
        }
    }
}