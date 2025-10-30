using Microsoft.EntityFrameworkCore;
using PodcastManagementSystem.Data;
using PodcastManagementSystem.Models;
using PodcastManagementSystem.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PodcastManagementSystem.Repositories
{
    public class PodcastRepository : IPodcastRepository
    {
        private readonly ApplicationDbContext _context;

        public PodcastRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- 1. Dashboard Listings ---
        public async Task<List<Podcast>> GetAllPodcastsAsync()
        {
            return await _context.Podcasts.ToListAsync();
        }

        public async Task<List<Episode>> GetPopularEpisodesAsync(int count = 10)
        {
            return await _context.Episodes
                                 .OrderByDescending(e => e.PlayCount)
                                 .Take(count)
                                 .ToListAsync();
        }

        public async Task<List<Episode>> GetRecentEpisodesAsync(int count = 10)
        {
            return await _context.Episodes
                                 .OrderByDescending(e => e.ReleaseDate)
                                 .Take(count)
                                 .ToListAsync();
        }

        // --- 2. Search ---
        public async Task<List<Episode>> SearchEpisodesAsync(string query, Guid? creatorId = null)
        {
            var episodes = _context.Episodes.Include(e => e.Podcast).AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                // Search by Title OR Podcast Description
                episodes = episodes.Where(e => e.Title.Contains(query) || e.Podcast.Description.Contains(query));
            }

            if (creatorId.HasValue)
            {
                // Filter by CreatorID
                episodes = episodes.Where(e => e.Podcast.CreatorID == creatorId.Value);
            }

            return await episodes.ToListAsync();
        }

        // --- 3. Episode/Subscription Management ---
        public async Task<Episode> GetEpisodeDetailsAsync(int episodeId)
        {
            return await _context.Episodes
                                 .Include(e => e.Podcast)
                                 .FirstOrDefaultAsync(e => e.EpisodeID == episodeId);
        }

        public async Task IncrementPlayCountAsync(int episodeId)
        {
            var episode = await _context.Episodes.FindAsync(episodeId);
            if (episode != null)
            {
                episode.PlayCount++;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> AddSubscriptionAsync(Guid userId, int podcastId)
        {
            if (await IsSubscribedAsync(userId, podcastId)) return false;

            var subscription = new Subscription
            {
                UserID = userId,
                PodcastID = podcastId,
                SubscribedDate = DateTime.Now
            };

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsSubscribedAsync(Guid userId, int podcastId)
        {
            return await _context.Subscriptions.AnyAsync(s => s.UserID == userId && s.PodcastID == podcastId);
        }

        // ---------------------------------------------------------------------
        // --- 4. Podcaster/Creator Management ---
        // ---------------------------------------------------------------------

        /// Podcast CRUD
        public async Task<List<Podcast>> GetPodcastsByCreatorIdAsync(Guid creatorId)
        {
            // Fetches all podcasts where the CreatorID matches the provided GUID
            return await _context.Podcasts
                .Where(p => p.CreatorID == creatorId)
                .ToListAsync();
        }

        public async Task AddPodcastAsync(Podcast podcast)
        {
            // Adds a new podcast entry to the database
            _context.Podcasts.Add(podcast);
            await _context.SaveChangesAsync();
        }

        public async Task<Podcast> GetPodcastByIdAsync(int podcastId)
        {
            // Fetches a single podcast by its primary key
            return await _context.Podcasts.FindAsync(podcastId);
        }

        public async Task DeletePodcastAsync(int podcastId)
        {
            // Find the podcast by ID
            var podcast = await _context.Podcasts.FindAsync(podcastId);

            if (podcast == null)
                return; // Nothing to delete

            // Optional: If you also store episodes, delete them first
            var episodes = _context.Episodes.Where(e => e.PodcastID == podcastId);
            _context.Episodes.RemoveRange(episodes);

            // Remove the podcast
            _context.Podcasts.Remove(podcast);

            // Commit changes to the database
            await _context.SaveChangesAsync();
        }


        /// Episode CRUD

        public async Task AddEpisodeAsync(Episode episode)
        {
            // Adds a new episode entry (with the S3 URL) to the database
            _context.Episodes.Add(episode);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateEpisodeAsync(Episode episode)
        {
            _context.Entry(episode).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Episode>> GetEpisodesByPodcastIdAsync(int podcastId)
        {
            // This retrieves all episodes where the foreign key PodcastID matches the one passed in.
            return await _context.Episodes
                .Where(e => e.PodcastID == podcastId)
                .OrderByDescending(e => e.ReleaseDate) // Order by latest release date
                .ToListAsync();
        }
    }
}



//using Microsoft.EntityFrameworkCore;
//using PodcastManagementSystem.Data;
//using PodcastManagementSystem.Models;
//using PodcastManagementSystem.Interfaces;
//using System.Linq;
//using System.Threading.Tasks;

//namespace PodcastManagementSystem.Repositories
//{
//    public class PodcastRepository : IPodcastRepository
//    {
//        private readonly ApplicationDbContext _context;

//        public PodcastRepository(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        // --- 1. Dashboard Listings ---
//        public async Task<List<Podcast>> GetAllPodcastsAsync()
//        {
//            return await _context.Podcasts.ToListAsync();
//        }

//        public async Task<List<Episode>> GetPopularEpisodesAsync(int count = 10)
//        {
//            return await _context.Episodes
//                                 .OrderByDescending(e => e.PlayCount)
//                                 .Take(count)
//                                 .ToListAsync();
//        }

//        public async Task<List<Episode>> GetRecentEpisodesAsync(int count = 10)
//        {
//            return await _context.Episodes
//                                 .OrderByDescending(e => e.ReleaseDate)
//                                 .Take(count)
//                                 .ToListAsync();
//        }

//        // --- 2. Search ---
//        public async Task<List<Episode>> SearchEpisodesAsync(string query, string creatorId)
//        {
//            var episodes = _context.Episodes.Include(e => e.Podcast).AsQueryable();

//            if (!string.IsNullOrWhiteSpace(query))
//            {
//                // Search by Title OR Description
//                episodes = episodes.Where(e => e.Title.Contains(query) || e.Podcast.Description.Contains(query));
//            }

//            if (!string.IsNullOrWhiteSpace(creatorId))
//            {
//                // Search by Host/CreatorID
//                episodes = episodes.Where(e => e.Podcast.CreatorID == creatorId);
//            }

//            return await episodes.ToListAsync();
//        }

//        // --- 3. Episode/Subscription Management ---
//        public async Task<Episode> GetEpisodeDetailsAsync(int episodeId)
//        {
//            return await _context.Episodes.Include(e => e.Podcast)
//                                         .FirstOrDefaultAsync(e => e.EpisodeID == episodeId);
//        }

//        public async Task IncrementPlayCountAsync(int episodeId)
//        {
//            var episode = await _context.Episodes.FindAsync(episodeId);
//            if (episode != null)
//            {
//                episode.PlayCount++;
//                await _context.SaveChangesAsync();
//            }
//        }

//        public async Task<bool> AddSubscriptionAsync(string userId, int podcastId)
//        {
//            if (await IsSubscribedAsync(userId, podcastId)) return false;

//            var subscription = new Subscription
//            {
//                UserID = userId,
//                PodcastID = podcastId,
//                SubscribedDate = System.DateTime.Now
//            };

//            _context.Subscriptions.Add(subscription);
//            await _context.SaveChangesAsync();
//            return true;
//        }

//        public async Task<bool> IsSubscribedAsync(string userId, int podcastId)
//        {
//            return await _context.Subscriptions.AnyAsync(s => s.UserID == userId && s.PodcastID == podcastId);
//        }
//    }
//}
