using InnoTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InnoTrack.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        private int GetUserId()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claimValue) || !int.TryParse(claimValue, out int userId))
                throw new UnauthorizedAccessException("Invalid User Token.");
            return userId;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var result = await _dashboardService.GetGlobalDashboardStatsAsync();
            return Ok(result);
        }

        [HttpGet("popular-projects")]
        public async Task<IActionResult> GetPopularProjects([FromQuery] int limit = 5)
        {
            var result = await _dashboardService.GetPopularProjectsAsync(limit);
            return Ok(result);
        }

        [HttpGet("trending-technologies")]
        public async Task<IActionResult> GetTrendingTechnologies()
        {
            var result = await _dashboardService.GetTrendingTechnologiesAsync();
            return Ok(result);
        }

        [HttpGet("student/current-originality-widget")]
        public async Task<IActionResult> GetCurrentOriginalityWidget()
        {
            var userId = GetUserId();
            var result = await _dashboardService.GetCurrentOriginalityWidget(userId);
            return Ok(result);
        }

        [HttpGet("student/project-status-widget")]
        public async Task<IActionResult> GetProjectStatusWidget()
        {
            var userId = GetUserId();
            var result = await _dashboardService.GetProjectStatusWidget(userId);
            return Ok(result);
        }

        [HttpGet("most-original")]
        public async Task<IActionResult> GetMostOriginalProjects([FromQuery] bool thisYearOnly = true, [FromQuery] int limit = 4)
        {
            var result = await _dashboardService.GetMostOriginalProjectsAsync(thisYearOnly, limit);
            return Ok(result);
        }
    }
}