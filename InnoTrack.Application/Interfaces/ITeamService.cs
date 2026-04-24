using InnoTrack.Application.DTOs.Teams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.Interfaces
{
    public interface ITeamService
    {
        Task<TeamResponseDto> CreateTeamAsync(int leaderStudentId, CreateTeamDto dto);
    }
}
