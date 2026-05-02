using InnoTrack.API.Attributes;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities.Enums;
using Microsoft.AspNetCore.Mvc;

namespace InnoTrack.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AuthorizeRoles(UserRole.Admin)]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService) => _analyticsService = analyticsService;

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {

            return Ok(await _analyticsService.GetSystemAnalyticsAsync());
        }
    }
}
