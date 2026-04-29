using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.Interfaces
{
    public interface IProjectAnalysisQueue
    {
        ValueTask QueueProjectAsync(int projectId);
        IAsyncEnumerable<int> DequeueAsync(CancellationToken token);
    }
}
