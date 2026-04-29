using InnoTrack.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace InnoTrack.Application.Services
{
    public class ProjectAnalysisQueue : IProjectAnalysisQueue
    {
        private readonly Channel<int> _queue;

        public ProjectAnalysisQueue()
        {
            var options = new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait };
            _queue = Channel.CreateBounded<int>(options);
        }

        public async ValueTask QueueProjectAsync(int projectId) => await _queue.Writer.WriteAsync(projectId);
        public IAsyncEnumerable<int> DequeueAsync(CancellationToken token) => _queue.Reader.ReadAllAsync(token);
    }
}
