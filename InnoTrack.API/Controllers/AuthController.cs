using InnoTrack.Application.DTOs.Auth;
using InnoTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace InnoTrack.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            var response = await _authService.RegisterAsync(request);
            return CreatedAtAction(nameof(Login), response);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            await _authService.ForgotPasswordAsync(dto);
            return Ok(new { message = "If the email is registered, a reset code will be sent. Please check your Junk or Spam folder if you don't see the email in your inbox." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            try
            {
                await _authService.ResetPasswordAsync(dto);
                return Ok(new { message = "Password has been reset successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _authService.LogoutAsync(userId);
            return NoContent();
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            var response = await _authService.RefreshTokenAsync(request.AccessToken, request.RefreshToken);
            return Ok(response);
        }
    }
}
