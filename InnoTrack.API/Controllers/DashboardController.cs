using InnoTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    }
}