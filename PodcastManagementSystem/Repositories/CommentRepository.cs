using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.Extensions.Configuration;
using PodcastManagementSystem.Interfaces;
using PodcastManagementSystem.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PodcastManagementSystem.Repositories
{
    public class CommentRepository : ICommentRepository
    {
        private readonly IDynamoDBContext _dbContext;
        private readonly string _tableName;

        public CommentRepository(IDynamoDBContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _tableName = configuration["AWS:DynamoDB:CommentsTableName"];
        }

        public async Task AddCommentAsync(Comment comment)
        {
            // Save the comment to the DynamoDB table
            await _dbContext.SaveAsync(comment);
        }

        public async Task<List<Comment>> GetCommentsByEpisodeAsync(int episodeId)
        {
            
            var conditions = new List<ScanCondition>
        {
            new ScanCondition("EpisodeID", Amazon.DynamoDBv2.DocumentModel.ScanOperator.Equal, episodeId)
        };

            var search = _dbContext.ScanAsync<Comment>(conditions);
            return await search.GetRemainingAsync();
        }

        public async Task<Comment> GetCommentByIdAsync(string commentId)
        {
            return await _dbContext.LoadAsync<Comment>(commentId);
        }

        public async Task<bool> UpdateCommentAsync(Comment comment)
        {
            await _dbContext.SaveAsync(comment);
            return true;
        }
    }
}
