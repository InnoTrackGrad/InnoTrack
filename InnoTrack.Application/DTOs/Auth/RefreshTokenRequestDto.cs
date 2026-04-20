using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.DTOs.Auth
{
    public record RefreshTokenRequestDto(string AccessToken, string RefreshToken);
}
