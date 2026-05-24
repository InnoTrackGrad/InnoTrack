using InnoTrack.API.Attributes;
using InnoTrack.Application.DTOs.Projects;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Entities.Enums;
using InnoTrack.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InnoTrack.API.Controllers
{
    [Route("api/projects")]
    [ApiController]
    [Authorize]
    public class ProjectCatalogController : ControllerBase
    {
        private readonly IProjectCatalogService _catalogService;
        private readonly IUnitOfWork _unitOfWork;

        public ProjectCatalogController(IProjectCatalogService catalogService, IUnitOfWork unitOfWork)
        {
            _catalogService = catalogService;
            _unitOfWork = unitOfWork;
        }

        private int GetUserId()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claimValue) || !int.TryParse(claimValue, out int userId))
                throw new UnauthorizedAccessException("Invalid User Token.");
            return userId;
        }

        /// <summary>Browse the full project catalog with optional filters.</summary>
        [HttpGet]
        public async Task<IActionResult> GetProjects(
            [FromQuery] int? year,
            [FromQuery] string? status,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _catalogService.GetProjectsAsync(year, status, search, page, pageSize);
            return Ok(result);
        }

        /// <summary>Get full details of a single project from the catalog.</summary>
        [HttpGet("{projectId:int}")]
        public async Task<IActionResult> GetProjectById(int projectId)
        {
            var result = await _catalogService.GetProjectByIdAsync(projectId);
            return Ok(result);
        }

        /// <summary>Get all available supervisors to choose from during submission.</summary>
        [HttpGet("supervisors")]
        public async Task<IActionResult> GetSupervisors()
        {
            var result = await _catalogService.GetSupervisorsAsync();
            return Ok(result);
        }

        /// <summary>Get the authenticated student's project.</summary>
        [HttpGet("/api/my-project")]
        [AuthorizeRoles(UserRole.Student)]
        public async Task<IActionResult> GetMyProject()
        {
            var userId = GetUserId();
            var result = await _catalogService.GetMyProjectAsync(userId);
            return Ok(result);
        }

        /// <summary>Save a new project draft. Student must be a team leader.</summary>
        [HttpPost("drafts")]
        [AuthorizeRoles(UserRole.Student)]
        public async Task<IActionResult> SaveDraft([FromBody] SaveProjectDraftDto dto)
        {
            var userId = GetUserId();
            var result = await _catalogService.SaveDraftAsync(userId, dto);
            return CreatedAtAction(nameof(GetProjectById), new { projectId = result.Id }, result);
        }

        /// <summary>Update an existing project draft.</summary>
        [HttpPut("drafts/{draftId:int}")]
        [AuthorizeRoles(UserRole.Student)]
        public async Task<IActionResult> UpdateDraft(int draftId, [FromBody] SaveProjectDraftDto dto)
        {
            var userId = GetUserId();
            var result = await _catalogService.UpdateDraftAsync(draftId, userId, dto);
            return Ok(result);
        }

        /// <summary>Delete a draft permanently.</summary>
        [HttpDelete("drafts/{draftId:int}")]
        [AuthorizeRoles(UserRole.Student)]
        public async Task<IActionResult> DeleteDraft(int draftId)
        {
            var userId = GetUserId();
            await _catalogService.DeleteDraftAsync(draftId, userId);
            return NoContent();
        }

        /// <summary>Run an AI similarity check on project content before formal submission.</summary>
        [HttpPost("similarity-check")]
        [AuthorizeRoles(UserRole.Student)]
        public async Task<IActionResult> RunSimilarityCheck([FromBody] SimilarityCheckRequestDto dto)
        {
            var result = await _catalogService.RunSimilarityCheckAsync(dto);
            return Ok(result);
        }

        /// <summary>Update limited details of an existing project.</summary>
        [HttpPatch("{projectId:int}/details")]
        [AuthorizeRoles(UserRole.Student)]
        public async Task<IActionResult> UpdateProjectDetails(int projectId, [FromBody] UpdateProjectDetailsDto dto)
        {
            var userId = GetUserId();
            await _catalogService.UpdateProjectDetailsAsync(projectId, userId, dto);
            return NoContent();
        }

        /// <summary>Recall a submitted project back to Draft status.</summary>
        [HttpDelete("{projectId:int}/submission")]
        [AuthorizeRoles(UserRole.Student)]
        public async Task<IActionResult> RecallSubmission(int projectId)
        {
            var userId = GetUserId();
            await _catalogService.RecallSubmissionAsync(projectId, userId);
            return Ok(new { projectId, status = "draft" });
        }

        [AllowAnonymous]
        [HttpGet("showcase")]
        public async Task<IActionResult> GetPublicShowcase()
        {
            var showcaseProjects = await _unitOfWork.Repository<Project>()
                .GetQueryable()
                .Where(p => p.IsPublicShowcase)
                .Include(p => p.Team).ThenInclude(t => t.Members).ThenInclude(m => m.Student)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Abstract,
                    p.OriginalityScore,
                    Students = p.Team.Members.Select(m => m.Student.FullName).ToList()
                })
                .ToListAsync();

            return Ok(showcaseProjects);
        }
    }
}