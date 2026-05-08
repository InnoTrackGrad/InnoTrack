using InnoTrack.API.Attributes;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InnoTrack.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;

        public StudentsController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        private int GetUserId()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claimValue) || !int.TryParse(claimValue, out int userId))
                throw new UnauthorizedAccessException("Invalid User Token.");
            return userId;
        }

        [HttpGet("me")]
        [AuthorizeRoles(UserRole.Student)]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetUserId();
            var result = await _studentService.GetStudentProfileAsync(userId);
            return Ok(result);
        }

        [HttpGet("{studentId:int}")]
        [AuthorizeRoles(UserRole.Student)]
        public async Task<IActionResult> GetStudentProfile(int studentId)
        {
            var result = await _studentService.GetPublicStudentProfileAsync(studentId);
            return Ok(result);
        }
    }
}