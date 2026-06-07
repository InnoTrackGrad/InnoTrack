using InnoTrack.API.Attributes;
using InnoTrack.Application.DTOs.Admin;
using InnoTrack.Application.DTOs.Professors;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Entities.Enums;
using InnoTrack.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InnoTrack.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AuthorizeRoles(UserRole.Admin)]
    public class AdminController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProfessorAdminService _professorAdminService;
        private readonly IAcademicYearService _academicYearService;
        private readonly IAuditService _auditService;


        public AdminController(
            IStudentService studentService,
            IUnitOfWork unitOfWork,
            IProfessorAdminService professorAdminService,
            IAcademicYearService academicYearService,
            IAuditService auditService)
        {
            _studentService = studentService;
            _unitOfWork = unitOfWork;
            _professorAdminService = professorAdminService;
            _academicYearService = academicYearService;
            _auditService = auditService;
        }

        private int GetUserId()
        {
            var v = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(v) || !int.TryParse(v, out int id))
                throw new UnauthorizedAccessException("Invalid User Token.");
            return id;
        }


        // ── Students ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Gets a list of all students in the system (Admin Only).
        /// </summary>
        [HttpGet("students")]
        public async Task<IActionResult> GetAllStudents()
        {
            var students = await _studentService.GetAllStudentsForAdminAsync();
            return Ok(students);
        }


        // ── Projects ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Toggles the showcase status of a project. Only projects with Approved status and originality score of 85 or higher can be showcased.
        /// </summary>
        /// <param name="projectId">The ID of the project to toggle.</param>
        /// <returns>A message indicating whether the project was added to or removed from the showcase.</returns>
        /// <response code="200">Showcase status toggled successfully.</response>
        /// <response code="400">Project cannot be showcased due to status or score requirements.</response>
        /// <response code="404">Project not found.</response>
        [HttpPatch("projects/{projectId:int}/toggle-showcase")]
        public async Task<IActionResult> ToggleShowcase(int projectId)
        {
            var adminId = GetUserId();
            var project = await _unitOfWork.Repository<Project>().GetByIdAsync(projectId)
                ?? throw new KeyNotFoundException("Project not found.");

            if (project.Status != ProjectStatus.Approved || project.OriginalityScore < 85)
                return BadRequest(new
                {
                    error = "Only approved projects with an originality score ≥ 85 can be showcased."
                });

            project.IsPublicShowcase = !project.IsPublicShowcase;
            _unitOfWork.Repository<Project>().Update(project);
            await _unitOfWork.CompleteAsync();

            _auditService.LogAction(adminId, "Toggle Showcase",
                $"Project {projectId} showcase → {project.IsPublicShowcase}.");

            return Ok(new { message = project.IsPublicShowcase ? "Added to showcase." : "Removed from showcase." });
        }


        // ── Professors ────────────────────────────────────────────────────────────

        /// <summary>
        /// Provisions a new professor account.
        /// This is the ONLY way professor accounts are created —
        /// the public /auth/register endpoint creates Students exclusively.
        /// </summary>
        [HttpPost("professors")]
        public async Task<IActionResult> CreateProfessor([FromBody] CreateProfessorDto dto)
        {
            var adminId = GetUserId();
            var result = await _professorAdminService.CreateProfessorAsync(dto);

            _auditService.LogAction(adminId, "Create Professor",
                $"Provisioned professor: {dto.Email} → ID {result.Id}.");

            return CreatedAtAction(nameof(GetProfessorById), new { id = result.Id }, result);
        }

        /// <summary>Returns all professor accounts with current team load info.</summary>
        [HttpGet("professors")]
        public async Task<IActionResult> GetAllProfessors()
        {
            var professors = await _professorAdminService.GetAllProfessorsAsync();
            return Ok(professors);
        }

        /// <summary>Returns full details for a single professor.</summary>
        [HttpGet("professors/{professorId:int}")]
        public async Task<IActionResult> GetProfessorById(int professorId)
        {
            var professor = await _professorAdminService.GetProfessorByIdAsync(professorId);
            return Ok(professor);
        }

        /// <summary>
        /// Updates a professor's name, department, team capacity, or active status.
        /// All fields optional — only provided fields are applied.
        /// </summary>
        [HttpPut("professors/{professorId:int}")]
        public async Task<IActionResult> UpdateProfessor(
            int professorId, [FromBody] UpdateProfessorAdminDto dto)
        {
            var adminId = GetUserId();
            await _professorAdminService.UpdateProfessorAsync(professorId, dto);

            _auditService.LogAction(adminId, "Update Professor", $"Updated professor ID {professorId}.");

            return NoContent();
        }

        /// <summary>Activates or deactivates a professor account without deleting it.</summary>
        [HttpPatch("professors/{professorId:int}/status")]
        public async Task<IActionResult> SetProfessorStatus(
            int professorId, [FromBody] SetActiveStatusDto dto)
        {
            var adminId = GetUserId();
            await _professorAdminService.SetProfessorActiveStatusAsync(professorId, dto.IsActive);

            _auditService.LogAction(adminId, "Professor Status",
                $"Professor {professorId} active → {dto.IsActive}.");

            return NoContent();
        }

        /// <summary>
        /// Resets a professor's password. Used when the professor
        /// cannot use the standard forgot-password flow.
        /// </summary>
        [HttpPatch("professors/{professorId:int}/reset-password")]
        public async Task<IActionResult> ResetProfessorPassword(
            int professorId, [FromBody] AdminResetPasswordDto dto)
        {
            var adminId = GetUserId();
            await _professorAdminService.ResetProfessorPasswordAsync(professorId, dto.NewPassword);

            _auditService.LogAction(adminId, "Reset Professor Password",
                $"Admin reset password for professor ID {professorId}.");

            return NoContent();
        }

        // ── Academic Years ────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new academic year. It is created inactive;
        /// use PATCH .../activate to make it the current year.
        /// </summary>
        [HttpPost("academic-years")]
        public async Task<IActionResult> CreateAcademicYear([FromBody] CreateAcademicYearDto dto)
        {
            var adminId = GetUserId();
            var result = await _academicYearService.CreateAsync(dto);

            _auditService.LogAction(adminId, "Create Academic Year",
                $"Created academic year '{dto.Name}' (ID {result.Id}).");

            return CreatedAtAction(nameof(GetAllAcademicYears), null, result);
        }

        /// <summary>Returns all academic years ordered by start date descending.</summary>
        [HttpGet("academic-years")]
        public async Task<IActionResult> GetAllAcademicYears()
        {
            var years = await _academicYearService.GetAllAsync();
            return Ok(years);
        }

        /// <summary>Returns the currently active academic year, or 404 if none is set.</summary>
        [HttpGet("academic-years/active")]
        public async Task<IActionResult> GetActiveAcademicYear()
        {
            var year = await _academicYearService.GetActiveAsync();
            if (year is null) return NotFound(new { error = "No active academic year is configured." });
            return Ok(year);
        }

        /// <summary>
        /// Makes the specified academic year the active one.
        /// Any previously active year is automatically deactivated.
        /// Only one academic year may be active at a time.
        /// </summary>
        [HttpPatch("academic-years/{academicYearId:int}/activate")]
        public async Task<IActionResult> ActivateAcademicYear(int academicYearId)
        {
            var adminId = GetUserId();
            await _academicYearService.ActivateAsync(academicYearId);

            _auditService.LogAction(adminId, "Activate Academic Year",
                $"Academic year {academicYearId} set as active.");

            return NoContent();
        }

        /// <summary>Updates the name or date range of an academic year.</summary>
        [HttpPut("academic-years/{academicYearId:int}")]
        public async Task<IActionResult> UpdateAcademicYear(
            int academicYearId, [FromBody] UpdateAcademicYearDto dto)
        {
            await _academicYearService.UpdateAsync(academicYearId, dto);
            return NoContent();
        }
    }
}
