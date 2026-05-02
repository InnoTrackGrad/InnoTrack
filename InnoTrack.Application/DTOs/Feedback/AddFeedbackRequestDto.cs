using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.DTOs.Feedback
{
    public record AddFeedbackRequestDto(string Content);
    public record ReviewProjectRequestDto(bool Approve);
}
