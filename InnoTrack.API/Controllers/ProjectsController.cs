using InnoTrack.API.Attributes;
using InnoTrack.Application.DTOs.Projects;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace InnoTrack.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AuthorizeRoles(UserRole.Student)]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly IFileService _fileService;

        public ProjectsController(IProjectService projectService, IFileService fileService)
        {
            _projectService = projectService;
            _fileService = fileService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateProject(CreateProjectDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _projectService.CreateProjectAsync(userId, dto);
            return Ok(result);
        }

        [HttpPost("{projectId}/upload")]
        public async Task<IActionResult> UploadAttachment(int projectId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file was uploaded.");

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            using var stream = file.OpenReadStream();
            var attachment = await _fileService.UploadFileAsync(
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                projectId,
                userId
            );

            return Ok(attachment);
        }
    }
}
