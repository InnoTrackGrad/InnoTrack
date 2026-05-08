using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Entities.Enums;
using InnoTrack.Domain.Interfaces;

namespace InnoTrack.Application.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public FeedbackService(IUnitOfWork unitOfWork, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        public async Task AddFeedbackAsync(int professorId, int projectId, string content)
        {
            var project = await _unitOfWork.Repository<Project>().GetByIdAsync(projectId);
            if (project == null)
                throw new KeyNotFoundException("Project not found.");

            var feedback = new Feedback
            {
                ProfessorId = professorId,
                ProjectId = projectId,
                Content = content
            };
            await _unitOfWork.Repository<Feedback>().AddAsync(feedback);
            await _unitOfWork.CompleteAsync();

            var message = "A professor has added feedback to your project.";
            var notifType = NotificationType.Info;

            var teamMembers = await _unitOfWork.Repository<TeamMember>()
                .GetAllAsync(tm => tm.TeamId == project.TeamId);

            var notificationTasks = teamMembers.Select(member =>
                _notificationService.SendNotificationAsync(
                    member.StudentId, "New Feedback",
                    message,
                    notifType));

            await Task.WhenAll(notificationTasks);
        }
    }
}
