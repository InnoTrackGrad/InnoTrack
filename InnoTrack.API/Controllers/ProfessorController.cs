using InnoTrack.API.Attributes;
using InnoTrack.Application.DTOs.Feedback;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InnoTrack.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfessorController : ControllerBase
    {
        private readonly IProfessorProjectService _professorProjectService;
        private readonly IFeedbackService _feedbackService;
        private readonly IAuditService _auditService;

        public ProfessorController(IProfessorProjectService professorProjectService, IFeedbackService feedbackService, IAuditService auditService)
        {
            _professorProjectService = professorProjectService;
            _feedbackService = feedbackService;
            _auditService = auditService;
        }

        private int GetProfessorId()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claimValue) || !int.TryParse(claimValue, out int profId))
            {
                throw new UnauthorizedAccessException("Invalid User Token.");
            }
            return profId;
        }

        /// <summary>
        /// Reviews a submitted project by either approving or rejecting it.
        /// </summary>
        /// <param name="projectId">
        /// The identifier of the project to review.
        /// </param>
        /// <param name="request">
        /// The review request containing the professor's approval decision.
        /// </param>
        /// <returns>
        /// Returns a confirmation message after the project review is completed.
        /// </returns>
        [AuthorizeRoles(UserRole.Professor)]
        [HttpPost("projects/{projectId}/review")]
        public async Task<IActionResult> ReviewProject(int projectId, [FromBody] ReviewProjectRequestDto request)
        {
            var profId = GetProfessorId();
            await _professorProjectService.ReviewProjectAsync(profId, projectId, request.Approve);

            string decision = request.Approve ? "Approved" : "Rejected";
            _auditService.LogAction(
                profId,
                "Project Review",
                $"Professor formally {decision} project with ID: {projectId}");

            return Ok(new { Message = "Project reviewed successfully." });
        }

        /// <summary>
        /// Adds professor feedback to a project and notifies team members.
        /// </summary>
        /// <param name="projectId">
        /// The identifier of the project receiving feedback.
        /// </param>
        /// <param name="request">
        /// The feedback request containing the feedback content.
        /// </param>
        /// <returns>
        /// Returns a confirmation message after the feedback is added.
        /// </returns>
        [AuthorizeRoles(UserRole.Professor)]
        [HttpPost("projects/{projectId}/feedback")]
        public async Task<IActionResult> AddFeedback(int projectId, [FromBody] AddFeedbackRequestDto request)
        {
            var profId = GetProfessorId();
            await _feedbackService.AddFeedbackAsync(profId, projectId, request.Content);
            return Ok(new { Message = "Feedback added successfully." });
        }

        /// <summary>
        /// Retrieves projects currently pending review for the authenticated professor.
        /// </summary>
        /// <param name="pageNumber">
        /// The page number to retrieve. Default value is 1.
        /// </param>
        /// <param name="pageSize">
        /// The number of projects per page. Default value is 20.
        /// </param>
        /// <returns>
        /// Returns a paginated list of projects awaiting professor review.
        /// </returns>
        [AuthorizeRoles(UserRole.Professor)]
        [HttpGet("projects/pending")]
        public async Task<IActionResult> GetPendingProjects([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var profId = GetProfessorId();
            var result = await _professorProjectService.GetPendingProjectsAsync(profId, pageNumber, pageSize);
            return Ok(result);
        }
    }
}
