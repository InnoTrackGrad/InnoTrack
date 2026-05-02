using InnoTrack.Application.DTOs.Analytics;
using InnoTrack.Application.Interfaces;
using InnoTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Infrastructure.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly ApplicationDbContext _context;
        public AnalyticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SystemAnalyticsDto> GetSystemAnalyticsAsync()
        {
            var totalUsers = await _context.Users.CountAsync(u => !u.IsDeleted);
            var totalTeams = await _context.Teams.CountAsync();
            var totalProjects = await _context.Projects.CountAsync();

            var averageScore = await _context.Projects
                .Where(p => p.OriginalityScore.HasValue)
                .AverageAsync(p => p.OriginalityScore) ?? 0;

            return new SystemAnalyticsDto(totalUsers, totalTeams, totalProjects, averageScore);
        }
    }
}
