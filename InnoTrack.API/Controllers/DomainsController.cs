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
    public class DomainsController : ControllerBase
    {
        private readonly IDomainService _domainService;
        private readonly IAuditService _auditService;

        public DomainsController(IDomainService domainService, IAuditService auditService)
        {
            _domainService = domainService;
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
            return Ok(await _domainService.GetAllDomainsAsync(pageNumber, pageSize));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            return Ok(await _domainService.GetDomainByIdAsync(id));
        }

        [AuthorizeRoles(UserRole.Admin)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDomainDto request)
        {
            var result = await _domainService.CreateDomainAsync(request);

            var adminId = GetAdminId();
            await _auditService.LogActionAsync(
                adminId,
                "Created Domain",
                $"Admin created a new Domain with ID: {result.Id}");

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [AuthorizeRoles(UserRole.Admin)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateDomainDto request)
        {
            await _domainService.UpdateDomainAsync(id, request);

            var adminId = GetAdminId();
            await _auditService.LogActionAsync(
                adminId,
                "Updated Domain",
                $"Admin updated Domain with ID: {id}");

            return NoContent();
        }

        [AuthorizeRoles(UserRole.Admin)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _domainService.DeleteDomainAsync(id);

            var adminId = GetAdminId();
            await _auditService.LogActionAsync(
                adminId,
                "Deleted Domain",
                $"Admin deleted Domain with ID: {id}");

            return NoContent();
        }

    }
}
