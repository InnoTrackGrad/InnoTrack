using InnoTrack.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.DTOs.Auth
{
    public record AuthResponseDto
        (string AccessToken, string RefreshToken, DateTime RefreshTokenExpiration, string Name, UserRole Role);
}
