using InnoTrack.Domain.Entities.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InnoTrack.Domain.Entities
{
    public class ChatMessage
    {
        public int Id { get; set; }

        [Required, Column(TypeName = "nvarchar(max)")]
        public string Content { get; set; } = null!;
        public MessageType Type { get; set; } = MessageType.Text;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int ChatRoomId { get; set; }
        public ChatRoom ChatRoom { get; set; } = null!;

        [Required]
        public int SenderId { get; set; }

        [ForeignKey(nameof(SenderId))]
        public User Sender { get; set; } = null!;
        public ICollection<ChatMessageAttachment> Attachments { get; set; } = new HashSet<ChatMessageAttachment>();

    }
}
