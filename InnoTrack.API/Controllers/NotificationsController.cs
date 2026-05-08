using InnoTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InnoTrack.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationReadService _notificationReadService;

        public NotificationsController(INotificationReadService notificationReadService)
        {
            _notificationReadService = notificationReadService;
        }

        private int GetUserId()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claimValue) || !int.TryParse(claimValue, out int userId))
                throw new UnauthorizedAccessException("Invalid User Token.");
            return userId;
        }

        /// <summary>Get the authenticated user's notification list, optionally filtered to unread only.</summary>
        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false)
        {
            var userId = GetUserId();
            var result = await _notificationReadService.GetNotificationsAsync(userId, unreadOnly);
            return Ok(result);
        }

        /// <summary>Mark a single notification as read.</summary>
        [HttpPatch("{notificationId:int}/read")]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            var userId = GetUserId();
            await _notificationReadService.MarkAsReadAsync(notificationId, userId);
            return NoContent();
        }

        /// <summary>Mark all of the user's notifications as read.</summary>
        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetUserId();
            await _notificationReadService.MarkAllAsReadAsync(userId);
            return NoContent();
        }
    }
}