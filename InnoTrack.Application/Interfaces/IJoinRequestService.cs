using InnoTrack.Application.DTOs.Teams;

namespace InnoTrack.Application.Interfaces
{
    public interface IJoinRequestService
    {
        Task RequestToJoinAsync(int studentId, int teamId);
        Task HandleRequestAsync(int leaderId, HandleRequestDto dto);
    }
}
