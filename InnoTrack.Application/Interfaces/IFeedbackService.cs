namespace InnoTrack.Application.Interfaces
{
    public interface IFeedbackService
    {
        Task AddFeedbackAsync(int professorId, int projectId, string content);
        Task MarkFeedbackAsReadAsync(int feedbackId, int userId);
    }
}
