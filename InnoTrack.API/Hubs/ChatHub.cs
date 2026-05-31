using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Entities.Enums;
using InnoTrack.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace InnoTrack.API.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ChatHub(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

        public async Task JoinTeamChat(int teamId)
        {
            var claimValue = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claimValue) || !int.TryParse(claimValue, out int userId))
            {
                throw new HubException("Unauthorized: Invalid user identity.");
            }

            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var isMember = await unitOfWork.Repository<TeamMember>()
                .FindAsync(tm => tm.TeamId == teamId && tm.StudentId == userId);

            if (isMember == null)
            {
                throw new HubException("Access Denied: You are not a member of this team.");
            }
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Team_{teamId}");
        }

        public async Task SendMessage(int teamId, string messageContent)
        {
            if (string.IsNullOrWhiteSpace(messageContent))
                throw new HubException("Message cannot be empty.");

            if (messageContent.Length > 2000)
                throw new HubException("Message exceeds the maximum allowed length of 2000 characters.");

            if (teamId <= 0)
                throw new HubException("Invalid team ID.");


            var claimValue = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claimValue) || !int.TryParse(claimValue, out int userId))
            {
                throw new HubException("Unauthorized: Invalid user identity.");
            }

            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var isMember = await unitOfWork.Repository<TeamMember>()
                .FindAsync(tm => tm.TeamId == teamId && tm.StudentId == userId);
            if (isMember == null)
            {
                throw new HubException("Access Denied: You cannot send messages to a team you do not belong to.");
            }

            var chatRoom = await unitOfWork.Repository<ChatRoom>().FindAsync(c => c.TeamId == teamId);
            if (chatRoom != null)
            {
                var message = new ChatMessage
                {
                    ChatRoomId = chatRoom.Id,
                    SenderId = userId,
                    Content = messageContent,
                    Type = MessageType.Text,
                    SentAt = DateTime.UtcNow
                };
                await unitOfWork.Repository<ChatMessage>().AddAsync(message);
                await unitOfWork.CompleteAsync();

                await Clients.GroupExcept($"Team_{teamId}", Context.ConnectionId).SendAsync("ReceiveMessage", new
                {
                    senderId = userId,
                    content = messageContent,
                    sentAt = message.SentAt
                });
            }
        }
    }
}
