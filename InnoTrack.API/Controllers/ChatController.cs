using InnoTrack.API.Attributes;
using InnoTrack.Application.DTOs.Chat;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InnoTrack.API.Controllers
{
    [Route("api/teams/me/chat")]
    [ApiController]
    [AuthorizeRoles(UserRole.Student)]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        private int GetUserId()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claimValue) || !int.TryParse(claimValue, out int userId))
                throw new UnauthorizedAccessException("Invalid User Token.");
            return userId;
        }

        /// <summary>Get the team chat workspace including messages and member list.</summary>
        [HttpGet]
        public async Task<IActionResult> GetTeamChat()
        {
            var userId = GetUserId();
            var result = await _chatService.GetTeamChatAsync(userId);
            return Ok(result);
        }

        /// <summary>Send a text message in the team chat via REST (SignalR hub also available).</summary>
        [HttpPost("messages")]
        public async Task<IActionResult> SendMessage([FromBody] SendChatMessageDto dto)
        {
            var userId = GetUserId();
            var result = await _chatService.SendMessageAsync(userId, dto.Content);
            return CreatedAtAction(nameof(GetTeamChat), result);
        }
    }
}