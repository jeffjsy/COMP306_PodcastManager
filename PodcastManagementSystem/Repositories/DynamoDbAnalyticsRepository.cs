using Amazon.DynamoDBv2.DataModel;
using Microsoft.EntityFrameworkCore;
using PodcastManagementSystem.Interfaces;

namespace PodcastManagementSystem.Repositories
{
    public class DynamoDbAnalyticsRepository : IAnalyticsRepository
    {
        private readonly IDynamoDBContext _dynamoDbContext;

        public DynamoDbAnalyticsRepository(IDynamoDBContext dynamoDbContext)
        {
            _dynamoDbContext = dynamoDbContext;
        }

        public async Task RecordViewAsync(int episodeId, Guid? userId, string ipAddress)
        {
            var viewEvent = new ViewEvent
            {
                EpisodeID = episodeId,
                TimeStamp = DateTime.UtcNow.ToString("o") + "-" + Guid.NewGuid().ToString("N").Substring(0, 4),
                UserID = userId,
                IPAddress = ipAddress
            };
            await _dynamoDbContext.SaveAsync(viewEvent);
        }

        public async Task<List<EpisodeSummary>> GetEpisodeSummariesByPodcastIdAsync(int podcastId)
        {
            // Query DynamoDB by the Partition Key (PodcastID)
            var search = _dynamoDbContext.QueryAsync<EpisodeSummary>(podcastId);

            // Load all results into a list
            var summaries = await search.GetRemainingAsync();

            return summaries;
        }

        public async Task LogEpisodeViewAsync(int podcastId, int episodeId)
        {
            // The summary entity, which doubles as the DynamoDB item structure
            var summary = await _dynamoDbContext.LoadAsync<EpisodeSummary>(podcastId, episodeId);

            if (summary == null)
            {
                // 1. Item does not exist: Create a new summary record
                summary = new EpisodeSummary
                {
                    PodcastID = podcastId,
                    EpisodeID = episodeId,
                    ViewCount = 1, // Initialize with 1 view
                    CommentCount = 0, // Initialize comment count
                                      // Note: EpisodeTitle is fetched later in the controller, so it's null/empty here
                };
            }
            else
            {
                // 2. Item exists: Increment the view count
                summary.ViewCount += 1;
            }

            // Save the new or updated summary item to DynamoDB
            await _dynamoDbContext.SaveAsync(summary);
        }

        public async Task UpdateCommentCountAsync(int podcastId, int episodeId, int adjustment)
        {
            // Load the existing summary item (or null if it's the first comment)
            var summary = await _dynamoDbContext.LoadAsync<EpisodeSummary>(podcastId, episodeId);

            if (summary == null)
            {
                // Item does not exist: Initialize it (e.g., first comment on this episode)
                summary = new EpisodeSummary
                {
                    PodcastID = podcastId,
                    EpisodeID = episodeId,
                    ViewCount = 0,
                    CommentCount = adjustment, 
                };
            }
            else
            {
                // Item exists: Apply the adjustment
                summary.CommentCount += adjustment;
                // Ensure count never drops below zero
                if (summary.CommentCount < 0)
                {
                    summary.CommentCount = 0;
                }
            }

            // Save the new or updated summary item to DynamoDB
            await _dynamoDbContext.SaveAsync(summary);
        }

        public async Task DeleteEpisodeSummaryAsync(int podcastId, int episodeId)
        {
            // 1. Create a key object representing the item to delete (PodcastID is PK, EpisodeID is SK)
            var key = new EpisodeSummary { PodcastID = podcastId, EpisodeID = episodeId };

            // 2. Use the DynamoDB context to perform the deletion
            await _dynamoDbContext.DeleteAsync(key);
        }
    }
}
