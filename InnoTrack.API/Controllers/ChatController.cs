using InnoTrack.API.Attributes;
using InnoTrack.API.Hubs;
using InnoTrack.Application.DTOs.Chat;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace InnoTrack.API.Controllers
{
    [Route("api/teams/me/chat")]
    [ApiController]
    [AuthorizeRoles(UserRole.Student)]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(IChatService chatService, IHubContext<ChatHub> hubContext)
        {
            _chatService = chatService;
            _hubContext = hubContext;
        }

        private int GetUserId()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claimValue) || !int.TryParse(claimValue, out int userId))
                throw new UnauthorizedAccessException("Invalid User Token.");
            return userId;
        }

        /// <summary>
        /// Retrieves the current user's team chat workspace including members and recent messages.
        /// </summary>
        /// <returns>
        /// Returns the chat room details, team members, and the latest chat messages.
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> GetTeamChat()
        {
            var userId = GetUserId();
            var result = await _chatService.GetTeamChatAsync(userId);
            return Ok(result);
        }

        /// <summary>
        /// Sends a new text message to the team chat.
        /// </summary>
        /// <param name="dto">The message content to send.</param>
        /// <returns>
        /// Returns the created chat message information.
        /// </returns>
        [HttpPost("messages")]
        public async Task<IActionResult> SendMessage([FromBody] SendChatMessageDto dto)
        {
            var userId = GetUserId();
            var result = await _chatService.SendMessageAsync(userId, dto.Content);
            await _hubContext.Clients.Group($"Team_{result.TeamId}").SendAsync("ReceiveMessage", result);
            return CreatedAtAction(nameof(GetTeamChat), result);
        }
    }
}