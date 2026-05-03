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

        [AuthorizeRoles(UserRole.Professor)]
        [HttpPost("projects/{projectId}/feedback")]
        public async Task<IActionResult> AddFeedback(int projectId, [FromBody] AddFeedbackRequestDto request)
        {
            var profId = GetProfessorId();
            await _feedbackService.AddFeedbackAsync(profId, projectId, request.Content);
            return Ok(new { Message = "Feedback added successfully." });
        }

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
