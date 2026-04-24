using InnoTrack.Application.DTOs.Teams;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Entities.Enums;
using InnoTrack.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.Services
{
    public class JoinRequestService : IJoinRequestService
    {
        private readonly IUnitOfWork _unitOfWork;

        public JoinRequestService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task RequestToJoinAsync(int studentId, string joinCode)
        {
            var team = await _unitOfWork.Repository<Team>().FindAsync(t => t.JoinCode == joinCode);
            if (team == null) throw new KeyNotFoundException("Invalid Join Code.");

            if (await _unitOfWork.Repository<TeamMember>().AnyAsync(tm => tm.StudentId == studentId))
                throw new InvalidOperationException("You are already in a team.");

            var currentMembersCount = await _unitOfWork.Repository<TeamMember>().CountAsync(tm => tm.TeamId == team.Id);
            if (currentMembersCount >= team.MaxSize) throw new InvalidOperationException("Team is full.");

            if (await _unitOfWork.Repository<JoinRequest>().AnyAsync(r => r.StudentId == studentId && r.TeamId == team.Id && r.Status == RequestStatus.Pending))
                throw new InvalidOperationException("You already have a pending request for this team.");

            var request = new JoinRequest
            {
                StudentId = studentId,
                TeamId = team.Id,
                Status = RequestStatus.Pending
            };

            await _unitOfWork.Repository<JoinRequest>().AddAsync(request);

            var leader = await _unitOfWork.Repository<TeamMember>()
                .FindAsync(tm => tm.TeamId == team.Id && tm.Role == TeamMemberRole.Leader);
            
            if (leader != null)
            {
                var notification = new Notification
                {
                    UserId = leader.StudentId,
                    Title = "New Join Request",
                    Message = $"A new student wants to join your team ({team.Name}).",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                await _unitOfWork.Repository<Notification>().AddAsync(notification);
            }
            await _unitOfWork.CompleteAsync();
        }

        public async Task HandleRequestAsync(int leaderId, HandleRequestDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var request = await _unitOfWork.Repository<JoinRequest>().GetByIdAsync(dto.RequestId);
                if (request == null) throw new KeyNotFoundException("Request not found.");

                var leader = await _unitOfWork.Repository<TeamMember>().FindAsync(tm => tm.TeamId == request.TeamId && tm.StudentId == leaderId);
                if (leader == null || leader.Role != TeamMemberRole.Leader)
                    throw new UnauthorizedAccessException("Only the team leader can handle requests.");

                if (dto.Accept)
                {
                    request.Status = RequestStatus.Approved;
                    var newMember = new TeamMember
                    {
                        TeamId = request.TeamId,
                        StudentId = request.StudentId,
                        Role = TeamMemberRole.Member
                    };
                    await _unitOfWork.Repository<TeamMember>().AddAsync(newMember);

                    var otherRequests = await _unitOfWork.Repository<JoinRequest>()
                        .GetAllAsync(r => r.StudentId == request.StudentId && r.Id != request.Id);
                    
                    foreach (var otherRequest in otherRequests)
                    {
                        otherRequest.Status = RequestStatus.Rejected;
                        _unitOfWork.Repository<JoinRequest>().Update(otherRequest);
                    }
                }
                else
                {
                    request.Status = RequestStatus.Rejected;
                }
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}