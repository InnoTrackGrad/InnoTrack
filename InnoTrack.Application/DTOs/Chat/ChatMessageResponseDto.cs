namespace InnoTrack.Application.DTOs.Chat
{
    public record ChatMessageResponseDto(int Id, string AuthorName, string Content, DateTime SentAt);
}