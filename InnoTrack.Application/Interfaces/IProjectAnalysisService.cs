namespace InnoTrack.Application.Interfaces
{
    public interface IProjectAnalysisService
    {
        Task ProcessProjectAiReportAsync(int projectId);
    }
}
