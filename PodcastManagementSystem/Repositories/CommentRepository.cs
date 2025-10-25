using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.Extensions.Configuration;
using PodcastManagementSystem.Interfaces;
using PodcastManagementSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PodcastManagementSystem.Repositories
{
    public class CommentRepository : ICommentRepository
    {
        
        private readonly IDynamoDBContext _dbContext;

        public CommentRepository(IDynamoDBContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            // _tableName = configuration["AWS:DynamoDB:CommentsTableName"];
        }

        public async Task AddCommentAsync(Comment comment)
        {
            // SaveAsync handles both initial creation and update operations (upsert).
            await _dbContext.SaveAsync(comment);
        }


        public async Task<List<Comment>> GetCommentsByEpisodeIdAsync(int episodeId)
        {
            var conditions = new List<ScanCondition>
            {
                // This checks the EpisodeID attribute on all items.
                new ScanCondition("EpisodeID", ScanOperator.Equal, episodeId)
            };

            var search = _dbContext.ScanAsync<Comment>(conditions);
            return await search.GetRemainingAsync();
        }


        public async Task<Comment> GetCommentByIdAsync(string commentId, int episodeId)
        {

            return await _dbContext.LoadAsync<Comment>(commentId);
        }

        public async Task<bool> UpdateCommentAsync(Comment comment)
        {
            // SaveAsync handles the update operation.
            await _dbContext.SaveAsync(comment);
            return true;
        }
    }
}