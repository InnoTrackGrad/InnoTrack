using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.DTOs.Teams
{
    public record TeamResponseDto(int Id, string Name, string JoinCode, int MaxSize);
}
