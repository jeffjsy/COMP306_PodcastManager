using Microsoft.EntityFrameworkCore;
using PodcastManagementSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PodcastManagementSystem.Interfaces
{
    public interface IEpisodeRepository
    {
        // CREATE: Add a new episode to the database
        Task AddEpisodeAsync(Episode episode);

        // READ: Get a specific episode by its ID
        Task<Episode> GetEpisodeByIdAsync(int episodeId);

        // UPDATE: Update an existing episode's metadata
        Task UpdateEpisodeAsync(Episode episode);

        // DELETE: Remove an episode from the database
        Task DeleteEpisodeByIdAsync(int episodeId);

        // DELETE: Remove ALL episodes of given podcast
        Task DeleteAllEpisodesByPodcastIdAsync(int podcastId);

        // Gets the parent ID needed for redirection after deletion
        Task<int> GetPodcastIdForEpisodeAsync(int episodeId);

        Task<List<Episode>> GetEpisodesByPodcastIdAsync(int podcastId);

        Task<List<Episode>> SearchEpisodesAsync(int podcastId, string query, string searchBy);
    }
}