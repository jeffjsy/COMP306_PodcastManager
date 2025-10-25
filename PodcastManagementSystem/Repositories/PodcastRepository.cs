using Microsoft.EntityFrameworkCore;
using PodcastManagementSystem.Data;
using PodcastManagementSystem.Models;
using PodcastManagementSystem.Interfaces;
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
        public async Task<List<Episode>> SearchEpisodesAsync(string query, string creatorId)
        {
            var episodes = _context.Episodes.Include(e => e.Podcast).AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                // Search by Title OR Description
                episodes = episodes.Where(e => e.Title.Contains(query) || e.Podcast.Description.Contains(query));
            }

            if (!string.IsNullOrWhiteSpace(creatorId))
            {
                // Search by Host/CreatorID
                episodes = episodes.Where(e => e.Podcast.CreatorID == creatorId);
            }

            return await episodes.ToListAsync();
        }

        // --- 3. Episode/Subscription Management ---
        public async Task<Episode> GetEpisodeDetailsAsync(int episodeId)
        {
            return await _context.Episodes.Include(e => e.Podcast)
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

        public async Task<bool> AddSubscriptionAsync(string userId, int podcastId)
        {
            if (await IsSubscribedAsync(userId, podcastId)) return false;

            var subscription = new Subscription
            {
                UserID = userId,
                PodcastID = podcastId,
                SubscribedDate = System.DateTime.Now
            };

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsSubscribedAsync(string userId, int podcastId)
        {
            return await _context.Subscriptions.AnyAsync(s => s.UserID == userId && s.PodcastID == podcastId);
        }
    }
}
