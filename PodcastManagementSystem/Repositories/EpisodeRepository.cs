using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PodcastManagementSystem.Controllers;
using PodcastManagementSystem.Data;
using PodcastManagementSystem.Interfaces;
using PodcastManagementSystem.Models;
using PodcastManagementSystem.Models.ViewModels;
using PodcastManagementSystem.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NAudio;

namespace PodcastManagementSystem.Repositories
{
    public class EpisodeRepository : IEpisodeRepository

    
    {
        private readonly ApplicationDbContext _context;
        private readonly IAmazonS3 _s3Client;
        private readonly IS3Service _s3Service;
        private readonly string _bucketName = "comp306-podcast-media-bucket";
        private readonly ILogger<PodcasterController> _logger;

        public EpisodeRepository(
    ApplicationDbContext context,
    IAmazonS3 s3Client,
    IS3Service s3Service,
    ILogger<PodcasterController> logger)
        {
            _context = context;
            _s3Client = s3Client;
            _s3Service = s3Service;
            _logger = logger;
        }

        // CREATE
        public async Task<Episode> AddEpisodeAsync(AddEpisodeViewModel model)
        {
            // 1. Upload the file to S3
            var fileKey = $"episodes/{model.PodcastID}/{Guid.NewGuid()}-{model.AudioFile.FileName}";
            string audioFileUrl = await _s3Service.UploadFileAsync(model.AudioFile, fileKey);

            if (string.IsNullOrEmpty(audioFileUrl))
            {
                _logger.LogError("S3 service returned a NULL or empty URL for file key {Key}.", fileKey);
                throw new InvalidOperationException("Failed to upload audio file to storage.");
            }

            _logger.LogInformation("S3 upload successful (URL: {Url}). Saving episode metadata.", audioFileUrl);

            // 2. extract durationMinutes from model
            using var audioStream = model.AudioFile.OpenReadStream();
            using var reader = new NAudio.Wave.Mp3FileReader(audioStream);
            model.DurationMinutes = (Convert.ToInt32(reader.TotalTime.TotalMinutes) == 0) ? 1 : Convert.ToInt32(reader.TotalTime.TotalMinutes);


            // 3. Create episode entity
            var episode = new Episode
            {
                PodcastID = model.PodcastID,
                Title = model.Title,
                Description = model.Description,
                ReleaseDate = DateTime.UtcNow,
                AudioFileURL = audioFileUrl,
                DurationMinutes = model.DurationMinutes
            };

            // 3. Save to database
            _context.Episodes.Add(episode);
            await _context.SaveChangesAsync();

            return episode;
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
        public async Task DeleteEpisodeByIdAsync(int id) 
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

        public async Task DeleteAllEpisodesByPodcastIdAsync(int podcastId)
        {
            var episodes = _context.Episodes.Where(e => e.PodcastID == podcastId);
            foreach (var e in episodes)
            {
                //await _s3Service.DeleteFileAsync(e.AudioFileURL);
                await _s3Service.DeleteFileAsync(e.AudioFileURL);
                // 2. DB Deletion
                _context.Episodes.Remove(e);
            }


            await _context.SaveChangesAsync();

            //var episode = await _context.Episodes
            //    .FirstOrDefaultAsync(e => e.EpisodeID == id);

            //DeleteObjectResponse s3_episodeDeleteion_result = null;

            //if (episode != null)
            //{
            //    // 1. S3 Deletion

            //    if (!string.IsNullOrEmpty(episode.AudioFileURL))
            //    {
            //        var uri = new Uri(episode.AudioFileURL);
            //        var episodeKey = uri.AbsolutePath.TrimStart('/');

            //        var deleteRequest = new Amazon.S3.Model.DeleteObjectRequest
            //        {
            //            BucketName = _bucketName,
            //            Key = episodeKey
            //        };

            //        s3_episodeDeleteion_result = await _s3Client.DeleteObjectAsync(deleteRequest);
            //    }

            //// 2. DB Deletion
            //_context.Episodes.Remove(episode);
            //await _context.SaveChangesAsync();
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

        // READ (All Episodes)
        public async Task<IEnumerable<Episode>> GetAllEpisodesAsync()
        {
            try
            {
                _logger.LogInformation("TRACE: Retrieving all episodes from the database.");

                var episodes = await _context.Episodes
                    .Include(e => e.Podcast) // Include related Podcast info
                    .AsNoTracking()          // Improves performance for read-only operations
                    .ToListAsync();

                _logger.LogInformation($"TRACE: Retrieved {episodes.Count} episodes successfully.");

                return episodes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR: Failed to retrieve all episodes.");
                throw; // Re-throw to be handled by upper layers (controller/service)
            }
        }

        /// READ: get all episodes for a given podcast
        public async Task<List<Episode>> GetEpisodesByPodcastIdAsync(int podcastId)
        {
            try
            {
                _logger.LogInformation("Retrieving episodes for PodcastID {PodcastId}.", podcastId);

                var episodes = await _context.Episodes
                    .Where(e => e.PodcastID == podcastId)
                    .Include(e => e.Podcast)   // Optional: include related Podcast entity if needed
                    .AsNoTracking()            // Improve performance if you don't intend to update these entities
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} episodes for PodcastID {PodcastId}.", episodes.Count, podcastId);

                return episodes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve episodes for PodcastID {PodcastId}.", podcastId);
                throw;  // Propagate exception for upper layers to handle
            }
        }


    }
}