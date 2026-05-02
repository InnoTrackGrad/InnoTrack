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
    public class DepartmentsController : ControllerBase
    {
        private readonly IDepartmentService _departmentService;
        private readonly IAuditService _auditService;
        public DepartmentsController(IDepartmentService departmentService, IAuditService auditService)
        {
            _departmentService = departmentService;
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
            var result = await _departmentService.GetAllDepartmentsAsync(pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _departmentService.GetDepartmentByIdAsync(id);
            return Ok(result);
        }

        [AuthorizeRoles(UserRole.Admin)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDepartmentDto request)
        {
            var result = await _departmentService.CreateDepartmentAsync(request);

            var adminId = GetAdminId();
            await _auditService.LogActionAsync(
                adminId,
                "Created Department",
                $"Admin created a new Department with ID: {result.Id}");

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [AuthorizeRoles(UserRole.Admin)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateDepartmentDto request)
        {
            await _departmentService.UpdateDepartmentAsync(id, request);

            var adminId = GetAdminId();
            await _auditService.LogActionAsync(
                adminId,
                "Updated Department",
                $"Admin updated Department with ID: {id}");

            return NoContent();
        }

        [AuthorizeRoles(UserRole.Admin)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            //In the future, check if department has students at it or not
            await _departmentService.DeleteDepartmentAsync(id);

            var adminId = GetAdminId();
            await _auditService.LogActionAsync(
                adminId,
                "Deleted Department",
                $"Admin deleted Department with ID: {id}");

            return NoContent();
        }

    }
}
