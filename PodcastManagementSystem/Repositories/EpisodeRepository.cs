using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PodcastManagementSystem.Controllers;
using PodcastManagementSystem.Data;
using PodcastManagementSystem.Interfaces;
using PodcastManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PodcastManagementSystem.Repositories
{
    public class EpisodeRepository : IEpisodeRepository
    
    {
        private readonly ApplicationDbContext _context;
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName = "comp306-podcast-media-bucket";
        private readonly ILogger<PodcasterController> _logger;

        public EpisodeRepository(ApplicationDbContext context, IAmazonS3 s3Client)
        {
            _context = context;
            _s3Client = s3Client;
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
                .Include(e => e.Podcast)
                //.AsNoTracking() // Uncomment this and run if there is a mismatch between your local DB and cache 
                .FirstOrDefaultAsync(e => e.EpisodeID == episodeId);

        }

        // UPDATE
        public async Task UpdateEpisodeAsync(Episode episode)
        {
            _logger.LogInformation("TRACE 5: Inside UpdateEpisodeAsync. Calling SaveChanges.");
            // _context.Episodes.Update(episode);
            _context.Entry(episode).State = Microsoft.EntityFrameworkCore.EntityState.Modified;

            await _context.SaveChangesAsync();
            _logger.LogInformation("TRACE 6: SaveChanges() completed.");


        }

        // DELETE
        public async Task DeleteEpisodeAsync(int id) 
        {
            var episode = await _context.Episodes
                .FirstOrDefaultAsync(e => e.EpisodeID == id);

            DeleteObjectResponse s3_episodeDeleteion_result = null;

            if (episode != null)
            {
                // 1. S3 Deletion

                if (!string.IsNullOrEmpty(episode.AudioFileURL))
                {
                    var uri = new Uri(episode.AudioFileURL);
                    var episodeKey = uri.AbsolutePath.TrimStart('/');

                    var deleteRequest = new Amazon.S3.Model.DeleteObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = episodeKey 
                    };
                     
                    s3_episodeDeleteion_result = await _s3Client.DeleteObjectAsync(deleteRequest);
                }

                // 2. DB Deletion
                _context.Episodes.Remove(episode);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetPodcastIdForEpisodeAsync(int episodeId)
        {
            // Find the episode and select only its parent PodcastID
            var podcastId = await _context.Episodes
                .Where(e => e.EpisodeID == episodeId)
                .Select(e => e.PodcastID) // Select only the ID property
                .FirstOrDefaultAsync();    // Get the first result or 0 (int default)

           
            return podcastId;
        }
    }
}