using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.DTOs.Users
{
    public record ChangePasswordDto(string OldPassword, string NewPassword);
}
