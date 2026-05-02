using InnoTrack.Application.DTOs.Analytics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.Interfaces
{
    public interface IAnalyticsService
    {
        Task<SystemAnalyticsDto> GetSystemAnalyticsAsync();
    }
}
