using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InnoTrack.Domain.Entities
{
    public class Student : User
    {

        [Column(TypeName = "decimal(3,2)")]
        [Range(0, 4.0)]
        public decimal? GPA { get; set; }

        public TeamMember? TeamMember { get; set; }
        public ICollection<StudentSkill> StudentSkills { get; set; } = new HashSet<StudentSkill>();
        public ICollection<JoinRequest> JoinRequests { get; set; } = new HashSet<JoinRequest>();
    }
}
