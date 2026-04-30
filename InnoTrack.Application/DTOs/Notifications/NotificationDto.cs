using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.DTOs.Notifications
{
    public record NotificationDto(
        int Id, string Title, string Message,
        string Type, bool IsRead, DateTime CreatedAt);
}
