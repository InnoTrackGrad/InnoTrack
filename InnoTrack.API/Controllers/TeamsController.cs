using InnoTrack.API.Attributes;
using InnoTrack.Application.DTOs.Teams;
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
    public class TeamsController : ControllerBase
    {
        private readonly ITeamService _teamService;
        private readonly IJoinRequestService _joinRequestService;

        public TeamsController(ITeamService teamService, IJoinRequestService joinRequestService)
        {
            _teamService = teamService;
            _joinRequestService = joinRequestService;
        }

        [AuthorizeRoles(UserRole.Student)]
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateTeamDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            var result = await _teamService.CreateTeamAsync(userId, dto);
            return Ok(result);
        }

        [AuthorizeRoles(UserRole.Student)]
        [HttpPost("join-request")]
        public async Task<IActionResult> JoinRequest(JoinRequestDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            await _joinRequestService.RequestToJoinAsync(userId, dto.JoinCode);
            return Ok(new { message = "Request sent successfully." });
        }

        [AuthorizeRoles(UserRole.Student)]
        [HttpPost("handle-request")]
        public async Task<IActionResult> HandleRequest([FromBody] HandleRequestDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            await _joinRequestService.HandleRequestAsync(userId, dto);
            return Ok(new { message = "Request handled successfully." });
        }
    }
}
