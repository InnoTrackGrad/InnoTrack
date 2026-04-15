using InnoTrack.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Domain.Entities
{
    public class TeamMember
    {
        public int Id { get; set; }

        [Required]
        public int TeamId { get; set; }
        public Team Team { get; set; }

        [Required]
        public int StudentId { get; set; }
        public Student Student { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(25)]
        public TeamMemberRole Role { get; set; }
    }
}
