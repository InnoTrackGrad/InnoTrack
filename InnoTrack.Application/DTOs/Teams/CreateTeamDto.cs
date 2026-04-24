using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.DTOs.Teams
{
    public record CreateTeamDto(string Name, int MaxSize);
}
