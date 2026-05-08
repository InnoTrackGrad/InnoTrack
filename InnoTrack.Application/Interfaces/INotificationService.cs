using InnoTrack.Domain.Entities.Enums;

namespace InnoTrack.Application.Interfaces
{
    public interface INotificationService
    {
        Task SendNotificationAsync(int userId, string title, string message, NotificationType type);
    }
}
