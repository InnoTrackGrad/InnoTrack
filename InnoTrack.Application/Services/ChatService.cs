using InnoTrack.Application.DTOs.Chat;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Entities.Enums;
using InnoTrack.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InnoTrack.Application.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChatService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<TeamChatDto> GetTeamChatAsync(int userId)
        {
            var teamMember = await _unitOfWork.Repository<TeamMember>()
                .FindAsync(tm => tm.StudentId == userId);
            if (teamMember == null)
                throw new InvalidOperationException("You are not in a team.");

            var chatRoom = await _unitOfWork.Repository<ChatRoom>()
                .FindAsync(c => c.TeamId == teamMember.TeamId);
            if (chatRoom == null)
                throw new KeyNotFoundException("Chat room not found.");

            var project = await _unitOfWork.Repository<Project>()
                .FindAsync(p => p.TeamId == teamMember.TeamId);

            var allMembers = await _unitOfWork.Repository<TeamMember>()
                .GetQueryable()
                .Include(tm => tm.Student)
                .Where(tm => tm.TeamId == teamMember.TeamId)
                .ToListAsync();

            var memberDtos = allMembers
                    .Where(m => m.Student != null)
                    .Select(member =>
                    {
                        var initials = $"{member.Student!.FirstName[0]}{member.Student.LastName[0]}".ToUpperInvariant();
                        return new ChatMemberDto(member.StudentId, member.Student.FullName, member.Role.ToString(), initials);
                    }).ToList();

            var messages = await _unitOfWork.Repository<ChatMessage>()
                .GetQueryable()
                .Where(m => m.ChatRoomId == chatRoom.Id)
                .Include(m => m.Sender)
                .OrderByDescending(m => m.SentAt)
                .Take(50)
                .ToListAsync();

            messages.Reverse();

            var messageDtos = messages.Select(msg => new ChatMessageDetailDto(
                msg.Id,
                msg.SenderId,
                msg.Sender?.FullName ?? "Unknown",
                msg.Content,
                msg.SentAt
            )).ToList();

            return new TeamChatDto(
                chatRoom.Id,
                project?.Title,
                memberDtos.AsReadOnly(),
                messageDtos.AsReadOnly()
            );
        }

        public async Task<ChatMessageResponseDto> SendMessageAsync(int userId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Message content cannot be empty.");

            if (content.Length > 2000)
                throw new ArgumentException("Message cannot exceed 2000 characters.");

            var teamMember = await _unitOfWork.Repository<TeamMember>()
                .FindAsync(tm => tm.StudentId == userId);
            if (teamMember == null)
                throw new InvalidOperationException("You are not in a team.");

            var chatRoom = await _unitOfWork.Repository<ChatRoom>()
                .FindAsync(c => c.TeamId == teamMember.TeamId);
            if (chatRoom == null)
                throw new KeyNotFoundException("Chat room not found.");

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");

            var message = new ChatMessage
            {
                ChatRoomId = chatRoom.Id,
                SenderId = userId,
                Content = content,
                Type = MessageType.Text,
                SentAt = DateTime.UtcNow,
            };

            await _unitOfWork.Repository<ChatMessage>().AddAsync(message);
            await _unitOfWork.CompleteAsync();

            return new ChatMessageResponseDto(message.Id, chatRoom.TeamId, userId, user.FullName, message.Content, message.SentAt);
        }
    }
}