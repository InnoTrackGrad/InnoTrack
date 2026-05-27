using InnoTrack.API.Attributes;
using InnoTrack.Application.DTOs.Teams;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InnoTrack.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamsController : ControllerBase
    {
        private readonly ITeamService _teamService;
        private readonly ITeamReadService _teamReadService;
        private readonly IJoinRequestService _joinRequestService;

        public TeamsController(ITeamService teamService, ITeamReadService teamReadService, IJoinRequestService joinRequestService)
        {
            _teamService = teamService;
            _teamReadService = teamReadService;
            _joinRequestService = joinRequestService;
        }

        private int GetUserId()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claimValue) || !int.TryParse(claimValue, out int userId))
                throw new UnauthorizedAccessException("Invalid User Token.");
            return userId;
        }

        [AuthorizeRoles(UserRole.Student)]
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateTeamDto dto)
        {
            var userId = GetUserId();
            var result = await _teamService.CreateTeamAsync(userId, dto);
            return Ok(result);
        }

        [AuthorizeRoles(UserRole.Student)]
        [HttpPost("join-request")]
        public async Task<IActionResult> JoinRequest(JoinRequestDto dto)
        {
            var userId = GetUserId();
            await _joinRequestService.RequestToJoinAsync(userId, dto);
            return Ok(new { message = "Request sent successfully." });
        }

        [AuthorizeRoles(UserRole.Student)]
        [HttpPost("me/requests/{requestId:int}/handle")]
        public async Task<IActionResult> HandleJoinRequest([FromBody] HandleRequestDto dto)
        {
            var leaderId = GetUserId();
            await _joinRequestService.HandleRequestAsync(leaderId, dto);
            string action = dto.Accept ? "accepted" : "denied";
            return Ok(new { message = $"The join request has been successfully {action}." });
        }

        /// <summary>Get the current student's team details. Returns null if not in a team.</summary>
        [AuthorizeRoles(UserRole.Student)]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyTeam()
        {
            var userId = GetUserId();
            var result = await _teamReadService.GetMyTeamAsync(userId);
            return Ok(result);
        }

        /// <summary>Instantly join a team using a valid 6-digit join code.</summary>
        [AuthorizeRoles(UserRole.Student)]
        [HttpPost("join-by-code")]
        public async Task<IActionResult> DirectJoinByCode([FromBody] DirectJoinDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.JoinCode) || dto.JoinCode.Length != 6)
                return BadRequest("Invalid join code format.");

            var userId = GetUserId();
            var result = await _teamService.DirectJoinByCodeAsync(userId, dto.JoinCode);
            return Ok(result);
        }

        /// <summary>Get all pending join requests for the authenticated team leader.</summary>
        [AuthorizeRoles(UserRole.Student)]
        [HttpGet("me/join-requests")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var userId = GetUserId();
            var result = await _teamReadService.GetPendingJoinRequestsAsync(userId);
            return Ok(result);
        }

        [HttpGet("requests/count")]
        public async Task<IActionResult> GetPendingRequestsCount()
        {
            var leaderId = GetUserId();
            var count = await _teamReadService.GetPendingRequestsCountAsync(leaderId);
            return Ok(new { Count = count });
        }

        /// <summary>Regenerate a new unique join code for the team.</summary>
        [AuthorizeRoles(UserRole.Student)]
        [HttpPost("me/join-code/generate")]
        public async Task<IActionResult> GenerateJoinCode()
        {
            var userId = GetUserId();
            var result = await _teamService.RegenerateJoinCodeAsync(userId);
            return Ok(result);
        }

        /// <summary>Send an email invitation to a student. (Stub — email infrastructure not yet wired.)</summary>
        [AuthorizeRoles(UserRole.Student)]
        [HttpPost("me/invite")]
        public IActionResult InviteByEmail([FromBody] object request)
        {
            // TODO: Wire up an IEmailService when email infrastructure is available.
            return Ok(new { message = "Invitation sent." });
        }
    }
}
