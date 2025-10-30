using Microsoft.EntityFrameworkCore;
using PodcastManagementSystem.Data;
using PodcastManagementSystem.Interfaces;
using PodcastManagementSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace PodcastManagementSystem.Repositories
{
    public class EpisodeRepository : IEpisodeRepository
    {
        private readonly ApplicationDbContext _context;

        public EpisodeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // CREATE
        public async Task AddEpisodeAsync(Episode episode)
        {
            _context.Episodes.Add(episode);
            await _context.SaveChangesAsync();
        }

        // READ (Single Episode)
        public async Task<Episode> GetEpisodeByIdAsync(int episodeId)
        {
            return await _context.Episodes
                .FirstOrDefaultAsync(e => e.PodcastID == episodeId);
        }

        // UPDATE
        public async Task UpdateEpisodeAsync(Episode episode)
        {
            _context.Episodes.Update(episode);
            // Mark the state as modified if tracking is disabled, otherwise Update() is often sufficient
            // _context.Entry(episode).State = EntityState.Modified; 
            await _context.SaveChangesAsync();
        }

        // DELETE
        public async Task DeleteEpisodeAsync(int episodeId)
        {
            var episode = await GetEpisodeByIdAsync(episodeId);
            if (episode != null)
            {
                _context.Episodes.Remove(episode);
                await _context.SaveChangesAsync();
            }
        }

  
    }
}