using InnoTrack.Application.DTOs.Teams;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Entities.Enums;
using InnoTrack.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InnoTrack.Application.Services
{
    public class TeamReadService : ITeamReadService
    {
        private readonly IUnitOfWork _unitOfWork;

        public TeamReadService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<MyTeamDto?> GetMyTeamAsync(int userId)
        {
            var teamMember = await _unitOfWork.Repository<TeamMember>()
                .FindAsync(tm => tm.StudentId == userId);
            if (teamMember == null) return null;

            var team = await _unitOfWork.Repository<Team>()
                .GetByIdAsync(teamMember.TeamId);
            if (team == null) return null;

            var project = await _unitOfWork.Repository<Project>()
                .FindAsync(p => p.TeamId == team.Id);

            var allMembers = await _unitOfWork.Repository<TeamMember>()
                .GetAllAsync(tm => tm.TeamId == team.Id);

            var teamMemberIds = allMembers.Select(m => m.StudentId).ToList();
            var skillData = await _unitOfWork.Repository<StudentSkill>()
                .GetQueryable()
                .Where(ss => teamMemberIds.Contains(ss.StudentId))
                .Include(ss => ss.Skill)
                .GroupBy(ss => ss.StudentId)
                .ToDictionaryAsync(g => g.Key, g => g.Select(ss => ss.Skill.Name).ToList());

            var memberDetails = new List<TeamMemberDetailDto>();
            foreach (var member in allMembers)
            {
                var student = await _unitOfWork.Repository<Student>().GetByIdAsync(member.StudentId);
                if (student == null) continue;

                var skills = skillData.GetValueOrDefault(member.StudentId, new List<string>());

                memberDetails.Add(new TeamMemberDetailDto(
                    student.Id, student.FullName, member.Role.ToString(), student.Email, skills.AsReadOnly()
                ));
            }

            return new MyTeamDto(
                team.Id,
                team.Name,
                project?.Id,
                project?.Title,
                team.JoinCode,
                teamMember.Role == TeamMemberRole.Leader,
                memberDetails.AsReadOnly()
            );
        }

        public async Task<IReadOnlyList<PendingJoinRequestDetailDto>> GetPendingJoinRequestsAsync(int leaderId)
        {
            var leaderRecord = await _unitOfWork.Repository<TeamMember>()
                .FindAsync(tm => tm.StudentId == leaderId && tm.Role == TeamMemberRole.Leader);
            if (leaderRecord == null)
                throw new UnauthorizedAccessException("Only the team leader can view join requests.");

            var pendingRequests = await _unitOfWork.Repository<JoinRequest>()
                .GetAllAsync(r => r.TeamId == leaderRecord.TeamId && r.Status == RequestStatus.Pending);

            var result = new List<PendingJoinRequestDetailDto>();
            foreach (var request in pendingRequests)
            {
                var student = await _unitOfWork.Repository<Student>().GetByIdAsync(request.StudentId);
                if (student == null) continue;

                var department = await _unitOfWork.Repository<Department>().GetByIdAsync(student.DepartmentId);

                var studentSkills = await _unitOfWork.Repository<StudentSkill>()
                    .GetAllAsync(ss => ss.StudentId == student.Id);

                var skills = new List<string>();
                foreach (var ss in studentSkills)
                {
                    var skill = await _unitOfWork.Repository<Skill>().GetByIdAsync(ss.SkillId);
                    if (skill != null) skills.Add(skill.Name);
                }

                result.Add(new PendingJoinRequestDetailDto(
                    request.Id,
                    student.Id,
                    student.FullName,
                    department?.Name ?? string.Empty,
                    student.GPA,
                    skills.AsReadOnly(),
                    request.CreatedAt,
                    request.Message
                ));
            }

            return result.AsReadOnly();
        }

    }
}