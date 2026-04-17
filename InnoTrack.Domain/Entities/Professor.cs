using System.ComponentModel.DataAnnotations;

namespace InnoTrack.Domain.Entities
{
    public class Professor : User
    {

        [Required, MaxLength(100)]
        public string Specialization { get; set; }
        public ICollection<Team> SupervisedTeams { get; set; } = new HashSet<Team>();
        public ICollection<Feedback> Feedbacks { get; set; } = new HashSet<Feedback>();
    }
}
