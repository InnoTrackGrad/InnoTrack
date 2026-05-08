namespace InnoTrack.Application.DTOs.Chat
{
    public record TeamChatDto(
        int ChatId,
        string? ProjectTitle,
        IReadOnlyList<ChatMemberDto> Members,
        IReadOnlyList<ChatMessageDetailDto> Messages
    );

    public record ChatMemberDto(int Id, string FullName, string Role, string Initials);

    public record ChatMessageDetailDto(
        int Id,
        int AuthorId,
        string AuthorName,
        string Content,
        DateTime SentAt
    );
}