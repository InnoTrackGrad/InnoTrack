using AutoMapper;
using InnoTrack.Application.DTOs.Teams;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Entities.Enums;
using InnoTrack.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            if (alreadyInTeam != null) throw new InvalidOperationException("You are already a member of a team.");

            string code;
            do { code = _codeGenerator.GenerateJoinCode(); }
            while (await _unitOfWork.Repository<Team>().FindAsync(t => t.JoinCode == code) != null);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
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
                await _unitOfWork.CompleteAsync();

                await _unitOfWork.CommitTransactionAsync();
                return _mapper.Map<TeamResponseDto>(team);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
