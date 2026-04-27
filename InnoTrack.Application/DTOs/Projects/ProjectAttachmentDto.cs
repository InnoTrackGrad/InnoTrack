using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.DTOs.Projects
{
    public record ProjectAttachmentDto(int Id, string OriginalName, string FilePath);
}
