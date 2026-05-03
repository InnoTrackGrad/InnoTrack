using InnoTrack.API.Attributes;
using InnoTrack.Application.DTOs.Lookups;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InnoTrack.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TechnologiesController : ControllerBase
    {
        private readonly ITechnologyService _technologyService;
        private readonly IAuditService _auditService;

        public TechnologiesController(ITechnologyService technologyService, IAuditService auditService)
        {
            _technologyService = technologyService;
            _auditService = auditService;
        }

        private int GetAdminId()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claimValue) || !int.TryParse(claimValue, out int adminId))
            {
                throw new UnauthorizedAccessException("Invalid User Token.");
            }
            return adminId;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            return Ok(await _technologyService.GetAllTechnologiesAsync(pageNumber, pageSize));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            return Ok(await _technologyService.GetTechnologyByIdAsync(id));
        }

        [AuthorizeRoles(UserRole.Admin)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTechnologyDto request)
        {
            var result = await _technologyService.CreateTechnologyAsync(request);

            _auditService.LogAction(
                GetAdminId(),
                "Created Technology",
                $"Admin created a new Technology with ID: {result.Id}");

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [AuthorizeRoles(UserRole.Admin)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateTechnologyDto request)
        {
            await _technologyService.UpdateTechnologyAsync(id, request);

            var adminId = GetAdminId();
            _auditService.LogAction(
                adminId,
                "Updated Technology",
                $"Admin updated Technology with ID: {id}");

            return NoContent();
        }

        [AuthorizeRoles(UserRole.Admin)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _technologyService.DeleteTechnologyAsync(id);

            var adminId = GetAdminId();
            _auditService.LogAction(
                adminId,
                "Deleted Technology",
                $"Admin deleted Technology with ID: {id}");

            return NoContent();
        }
    }
}
