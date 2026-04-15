using InnoTrack.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Domain.Entities
{
    public class Technology
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }
       
        [MaxLength(50)]        
        public TechnologyCategory Category { get; set; }

        public ICollection<ProjectTechnology> ProjectTechnologies { get; set; } = new HashSet<ProjectTechnology>();

    }
}
