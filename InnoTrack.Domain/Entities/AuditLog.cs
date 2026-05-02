using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Domain.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Action { get; set; }
        public string Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
