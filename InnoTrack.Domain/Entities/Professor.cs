using System.ComponentModel.DataAnnotations;

namespace InnoTrack.Domain.Entities
{
    public class Professor : User
    {
        [Required]
        public int DepartmentId { get; set; }

        [Required, MaxLength(100)]
        public string Specialization { get; set; }
        public Department Department { get; set; }
        public ICollection<Team> SupervisedTeams { get; set; } = new HashSet<Team>();
        public ICollection<Feedback> Feedbacks { get; set; } = new HashSet<Feedback>();
    }
}
