using InnoTrack.Application.DTOs.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.Interfaces
{
    public interface IUserService
    {
        Task UpdateProfileAsync(int userId, UpdateProfileDto request);
        Task ChangePasswordAsync(int userId, ChangePasswordDto request);
    }
}
