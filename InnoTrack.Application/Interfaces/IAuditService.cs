using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.Interfaces
{
    public interface IAuditService
    {
        void LogAction(int userId, string action, string details);
    }
}
