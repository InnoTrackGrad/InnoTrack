using InnoTrack.Application.DTOs.Users;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;

        public UserService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
        }

        public async Task UpdateProfileAsync(int userId, UpdateProfileDto request)
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.DepartmentId = request.DepartmentId;

            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.CompleteAsync();
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordDto request)
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            if (!_passwordHasher.Verify(request.OldPassword, user.PasswordHash))
                throw new ArgumentException("Incorrect old password.");

            user.PasswordHash = _passwordHasher.Hash(request.NewPassword);

            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.CompleteAsync();
        }       
    }
}
