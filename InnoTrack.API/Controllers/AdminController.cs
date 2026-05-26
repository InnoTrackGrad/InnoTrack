using InnoTrack.API.Attributes;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Entities.Enums;
using InnoTrack.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InnoTrack.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AuthorizeRoles(UserRole.Admin)]
    public class AdminController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly IUnitOfWork _unitOfWork;


        public AdminController(IStudentService studentService, IUnitOfWork unitOfWork)
        {
            _studentService = studentService;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Gets a list of all students in the system (Admin Only).
        /// </summary>
        [HttpGet("students")]
        public async Task<IActionResult> GetAllStudents()
        {
            var students = await _studentService.GetAllStudentsForAdminAsync();
            return Ok(students);
        }

        [HttpPatch("projects/{projectId}/toggle-showcase")]
        public async Task<IActionResult> ToggleShowcase(int projectId)
        {
            var project = await _unitOfWork.Repository<Project>().GetByIdAsync(projectId);
            if (project == null) throw new KeyNotFoundException("Project not found.");

            if (project.Status != ProjectStatus.Approved || project.OriginalityScore < 85)
                return BadRequest("Only completed projects with high originality scores can be showcased.");

            project.IsPublicShowcase = !project.IsPublicShowcase;
            _unitOfWork.Repository<Project>().Update(project);
            await _unitOfWork.CompleteAsync();

            return Ok(new { message = project.IsPublicShowcase ? "Added to showcase." : "Removed from showcase." });
        }
    }
}
