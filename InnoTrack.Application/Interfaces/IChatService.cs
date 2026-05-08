using InnoTrack.Application.DTOs.Chat;

namespace InnoTrack.Application.Interfaces
{
    public interface IChatService
    {
        Task<TeamChatDto> GetTeamChatAsync(int userId);
        Task<ChatMessageResponseDto> SendMessageAsync(int userId, string content);
    }
}