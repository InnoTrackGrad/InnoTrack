using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Domain.Entities
{
    public class Team
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required, MaxLength(20)]
        public string JoinCode { get; set; }

        [Required, Range(1, 20)]
        public int MaxSize { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? ProfessorId { get; set; }
        public Professor Supervisor { get; set; }

        public Project Project { get; set; }
        public ChatRoom ChatRoom { get; set; }

        public ICollection<TeamMember> Members { get; set; } = new HashSet<TeamMember>();  
    }
}
