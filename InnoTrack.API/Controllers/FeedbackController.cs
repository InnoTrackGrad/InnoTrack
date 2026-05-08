using InnoTrack.API.Attributes;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InnoTrack.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AuthorizeRoles(UserRole.Student)]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackReadService _feedbackReadService;

        public FeedbackController(IFeedbackReadService feedbackReadService)
        {
            _feedbackReadService = feedbackReadService;
        }

        private int GetUserId()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claimValue) || !int.TryParse(claimValue, out int userId))
                throw new UnauthorizedAccessException("Invalid User Token.");
            return userId;
        }

        /// <summary>Get all professor feedback received for the student's project.</summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetMyFeedback()
        {
            var userId = GetUserId();
            var result = await _feedbackReadService.GetMyFeedbackAsync(userId);
            return Ok(result);
        }
    }
}