namespace InnoTrack.Application.DTOs.Chat
{
    public record ChatMessageResponseDto(int Id, int TeamId, int SenderId, string AuthorName, string Content, DateTime SentAt);
}