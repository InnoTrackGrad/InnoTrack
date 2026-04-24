using InnoTrack.Application.DTOs.Teams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.Interfaces
{
    public interface IJoinRequestService
    {
        Task RequestToJoinAsync(int studentId, string joinCode);
        Task HandleRequestAsync(int leaderId, HandleRequestDto dto);
    }
}
