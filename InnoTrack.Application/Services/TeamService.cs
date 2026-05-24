using AutoMapper;
using InnoTrack.Application.DTOs.Teams;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Entities.Enums;
using InnoTrack.Domain.Interfaces;

namespace InnoTrack.Application.Services
{
    public class TeamService : ITeamService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IJoinCodeGenerator _codeGenerator;

        public TeamService(IUnitOfWork unitOfWork, IMapper mapper, IJoinCodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _codeGenerator = codeGenerator;
        }

        public async Task<TeamResponseDto> CreateTeamAsync(int leaderStudentId, CreateTeamDto request)
        {
            var alreadyInTeam = await _unitOfWork.Repository<TeamMember>().FindAsync(tm => tm.StudentId == leaderStudentId);
            if (alreadyInTeam != null)
                throw new InvalidOperationException("You are already a member of a team.");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                string code;
                const int maxAttempts = 5;
                int attempt = 0;
                do
                {
                    if (++attempt > maxAttempts)
                        throw new InvalidOperationException("Failed to generate a unique join code. Try again.");
                    code = _codeGenerator.GenerateJoinCode();
                }
                while (await _unitOfWork.Repository<Team>().FindAsync(t => t.JoinCode == code) != null);

                var team = new Team
                {
                    Name = request.Name,
                    MaxSize = request.MaxSize,
                    JoinCode = code
                };
                await _unitOfWork.Repository<Team>().AddAsync(team);
                await _unitOfWork.CompleteAsync();

                var member = new TeamMember
                {
                    TeamId = team.Id,
                    StudentId = leaderStudentId,
                    Role = TeamMemberRole.Leader
                };
                await _unitOfWork.Repository<TeamMember>().AddAsync(member);

                var chatRoom = new ChatRoom
                {
                    TeamId = team.Id,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<ChatRoom>().AddAsync(chatRoom);

                await _unitOfWork.CommitTransactionAsync();

                return _mapper.Map<TeamResponseDto>(team);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<GenerateJoinCodeResponseDto> RegenerateJoinCodeAsync(int userId)
        {
            var leaderRecord = await _unitOfWork.Repository<TeamMember>()
                .FindAsync(tm => tm.StudentId == userId && tm.Role == TeamMemberRole.Leader);
            if (leaderRecord == null)
                throw new UnauthorizedAccessException("Only the team leader can regenerate the join code.");

            var team = await _unitOfWork.Repository<Team>().GetByIdAsync(leaderRecord.TeamId)
                ?? throw new KeyNotFoundException("Team not found.");

            string newCode;
            const int maxAttempts = 5;
            int attempt = 0;
            do
            {
                if (++attempt > maxAttempts)
                    throw new InvalidOperationException("Failed to generate a unique join code. Please try again.");

                newCode = _codeGenerator.GenerateJoinCode();
            }
            while (await _unitOfWork.Repository<Team>()
                .AnyAsync(t => t.JoinCode == newCode && t.Id != team.Id));

            team.JoinCode = newCode;
            team.JoinCodeExpiry = DateTime.UtcNow.AddHours(24);

            _unitOfWork.Repository<Team>().Update(team);
            await _unitOfWork.CompleteAsync();

            return new GenerateJoinCodeResponseDto(newCode);
        }

        public async Task<DirectJoinResponseDto> DirectJoinByCodeAsync(int userId, string joinCode)
        {
            var team = await _unitOfWork.Repository<Team>()
                .FindAsync(t => t.JoinCode == joinCode);
            if (team == null)
                throw new KeyNotFoundException("Invalid join code.");

            if (team.JoinCodeExpiry.HasValue && team.JoinCodeExpiry.Value < DateTime.UtcNow)
                throw new UnauthorizedAccessException("This join code has expired. Ask the leader to generate a new one.");

            if (await _unitOfWork.Repository<TeamMember>().AnyAsync(tm => tm.StudentId == userId))
                throw new InvalidOperationException("You are already in a team.");

            var currentCount = await _unitOfWork.Repository<TeamMember>()
                .CountAsync(tm => tm.TeamId == team.Id);
            if (currentCount >= team.MaxSize)
                throw new InvalidOperationException("Team is full.");

            await _unitOfWork.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var newMember = new TeamMember
                {
                    TeamId = team.Id,
                    StudentId = userId,
                    Role = TeamMemberRole.Member
                };
                await _unitOfWork.Repository<TeamMember>().AddAsync(newMember);

                var project = await _unitOfWork.Repository<Project>()
                    .FindAsync(p => p.TeamId == team.Id);

                var chatRoom = await _unitOfWork.Repository<ChatRoom>()
                    .FindAsync(c => c.TeamId == team.Id);

                await _unitOfWork.CommitTransactionAsync();

                return new DirectJoinResponseDto(
                    team.Id,
                    project?.Id,
                    chatRoom?.Id,
                    "Successfully joined the team."
                );
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

        }
    }
}
