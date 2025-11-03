namespace PodcastManagementSystem.Models.ViewModels
{
    public class EpisodeStatsViewModel
    {
        public string PodcastTitle { get; set; }
        public List<EpisodeSummary> TopEpisodes { get; set; }
    }
}
