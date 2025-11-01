using PodcastManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PodcastManagementSystem.Interfaces
{
    public interface IPodcastRepository
    {
        // 1. Dashboard Listings
        Task<List<Podcast>> GetAllPodcastsAsync();
        Task<List<Episode>> GetPopularEpisodesAsync(int count = 10);
        Task<List<Episode>> GetRecentEpisodesAsync(int count = 10);

        // 2. Search
        Task<List<Episode>> SearchEpisodesAsync(string query, Guid? creatorId = null);

        // 3. Episode/Subscription Management
        Task<Episode> GetEpisodeDetailsAsync(int episodeId);
        Task IncrementPlayCountAsync(int episodeId);
        Task<bool> AddSubscriptionAsync(Guid userId, int podcastId);
        Task<bool> IsSubscribedAsync(Guid userId, int podcastId);


        // --- 4. PODCASTER/CREATOR MANAGEMENT ---
        // Retrieve all podcasts owned by a specific creator
        Task<List<Podcast>> GetPodcastsByCreatorIdAsync(Guid creatorId);

        // CRUD operations for a Podcast (the channel)
        Task AddPodcastAsync(Podcast podcast);
        Task<Podcast> GetPodcastByIdAsync(int podcastId);
        Task DeletePodcastAsync(int podcastId);

        // CRUD operations for Episodes (the content)
        Task AddEpisodeAsync(Episode episode);
        Task UpdateEpisodeAsync(Episode episode); // For updating play count, title, etc.
        Task<IEnumerable<Episode>> GetEpisodesByPodcastIdAsync(int podcastId);
        // gets episodes to be approved (for Admin Approval Queue)
        Task<IEnumerable<Episode>> GetUnapprovedEpisodesByPodcastIdAsync(int podcastId);
        // gets episodes that are approved (for ListenerViewer viewing)
        Task<IEnumerable<Episode>> GetApprovedEpisodesByPodcastIdAsync(int podcastId);



    }
}



//using PodcastManagementSystem.Models;

//namespace PodcastManagementSystem.Interfaces
//{
//    public interface IPodcastRepository
//    {
//        // 1. Dashboard Listings
//        Task<List<Podcast>> GetAllPodcastsAsync();
//        Task<List<Episode>> GetPopularEpisodesAsync(int count = 10);
//        Task<List<Episode>> GetRecentEpisodesAsync(int count = 10);

//        // 2. Search
//        Task<List<Episode>> SearchEpisodesAsync(string query, string creatorId);

//        // 3. Episode/Subscription Management
//        Task<Episode> GetEpisodeDetailsAsync(int episodeId);
//        Task IncrementPlayCountAsync(int episodeId);
//        Task<bool> AddSubscriptionAsync(string userId, int podcastId);
//        Task<bool> IsSubscribedAsync(string userId, int podcastId);
//    }
//}
