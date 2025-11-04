using Microsoft.EntityFrameworkCore;
using PodcastManagementSystem.Models;
using PodcastManagementSystem.Models.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PodcastManagementSystem.Interfaces
{
    public interface IEpisodeRepository
    {
        // CREATE: Add a new episode to the database
        Task<Episode> AddEpisodeAsync(AddEpisodeViewModel episode);

        // READ: Get a specific episode by its ID
        Task<Episode> GetEpisodeByIdAsync(int episodeId);

        // ReadL Get all episodes
        Task<IEnumerable<Episode>> GetAllEpisodesAsync();

        // UPDATE: Update an existing episode's metadata
        Task UpdateEpisodeAsync(Episode episode);
        // UPDATE: Update an existing episode's CreationOfEpisodeApproved
        Task ApproveEpisodeByIdAsync(int episodeId);

        // DELETE: Remove an episode from the database
        Task DeleteEpisodeByIdAsync(int episodeId);

        // DELETE: Remove ALL episodes of given podcast
        Task DeleteAllEpisodesByPodcastIdAsync(int podcastId);

        // Gets the parent ID needed for redirection after deletion
        Task<int> GetPodcastIdForEpisodeAsync(int episodeId);

        Task<List<Episode>> GetEpisodesByPodcastIdAsync(int podcastId);
        // Get all unapproved episodes for podcast approval queue
        Task<IEnumerable<Episode>> GetAllUnapprovedEpisodesAsync();
    }
}