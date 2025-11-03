namespace PodcastManagementSystem.Interfaces
{
    public interface IAnalyticsRepository
    {
        Task RecordViewAsync(int episodeId, Guid? userId, string ipAddress);
        Task<List<EpisodeSummary>> GetEpisodeSummariesByPodcastIdAsync(int podcastId);

        Task LogEpisodeViewAsync(int podcastId, int episodeId);

        Task UpdateCommentCountAsync(int podcastId, int episodeId, int adjustment);
    }
}
