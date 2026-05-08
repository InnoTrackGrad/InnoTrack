namespace InnoTrack.Application.Interfaces
{
    public interface IProjectAnalysisQueue
    {
        ValueTask QueueProjectAsync(int projectId);
        IAsyncEnumerable<int> DequeueAsync(CancellationToken token);
    }
}
