using InnoTrack.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Domain.Entities
{
    public class Notification
    {
        public int Id { get; set; }

        [Required, MaxLength(255)]
        public string Message { get; set; } = null!;

        public NotificationType Type { get; set; } = NotificationType.Info;

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public int? ReferenceId { get; set; }
        public ReferenceType ReferenceType { get; set; } = ReferenceType.System;
    }
}
