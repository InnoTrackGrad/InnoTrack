using InnoTrack.Application.DTOs.Auth;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Entities.Enums;
using InnoTrack.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;

        public AuthService(IUnitOfWork unitOfWork, ITokenService tokenService)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            var existingUser = await _unitOfWork.Repository<User>().FindAsync(u => u.Email == request.Email);
            if (existingUser != null)
                throw new Exception("Email is already registered.");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newUser = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PasswordHash = passwordHash,
                DepartmentId = request.DepartmentId,
                CreatedAt = DateTime.UtcNow,
                Role = UserRole.Student
            };

            await _unitOfWork.Repository<User>().AddAsync(newUser);
            await _unitOfWork.CompleteAsync();

            var accessToken = _tokenService.GenerateAccessToken(newUser);
            var refreshToken = _tokenService.GenerateRefreshToken();

            newUser.RefreshToken = refreshToken;
            newUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            _unitOfWork.Repository<User>().Update(newUser);
            await _unitOfWork.CompleteAsync();

            return new AuthResponseDto(accessToken, refreshToken, newUser.RefreshTokenExpiryTime.Value, newUser.FullName, newUser.Role);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            var user = await _unitOfWork.Repository<User>().FindAsync(u => u.Email == request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Inavlid email or password.");

            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.CompleteAsync();

            return new AuthResponseDto(accessToken, refreshToken, user.RefreshTokenExpiryTime.Value, user.FullName, user.Role);

        }

        public Task<AuthResponseDto> RefreshTokenAsync(string token, string refreshToken)
        {
            throw new NotImplementedException();
        }
    }
}
