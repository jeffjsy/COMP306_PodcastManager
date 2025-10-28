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
