using Amazon.DynamoDBv2.DataModel;
using PodcastManagementSystem.Interfaces;
using PodcastManagementSystem.Models; 

public class DynamoDbCommentRepository : ICommentRepository
{
    private readonly IDynamoDBContext _dynamoDbContext;

    public DynamoDbCommentRepository(IDynamoDBContext dynamoDbContext)
    {
        _dynamoDbContext = dynamoDbContext;
    }

    // --- CREATE ---
    public async Task AddCommentAsync(Comment comment)
    {
        // SaveAsync handles the insertion of a new item based on the keys defined in the Comment model.
        await _dynamoDbContext.SaveAsync(comment);
    }

    // --- READ (Get List) ---
    public async Task<List<Comment>> GetCommentsByEpisodeIdAsync(int episodeId)
    {
        // Query DynamoDB by the Partition Key (EpisodeID).
        var search = _dynamoDbContext.QueryAsync<Comment>(episodeId);

        // Load all results into a list.
        var comments = await search.GetRemainingAsync();

        return comments;
    }

    // --- READ (Get Single) ---
    public async Task<Comment> GetCommentByIdAsync(int episodeId, Guid commentId)
    {
        return await _dynamoDbContext.LoadAsync<Comment>(episodeId, commentId);
    }

    // --- UPDATE ---
    public async Task UpdateCommentAsync(Comment comment)
    {
        // SaveAsync will overwrite the existing item, using the primary keys for identification.
        await _dynamoDbContext.SaveAsync(comment);
    }

    // --- DELETE ---
    public async Task DeleteCommentAsync(Comment comment)
    {
        // DeleteAsync uses the keys (EpisodeID and CommentID) from the object to remove the item.
        await _dynamoDbContext.DeleteAsync(comment);
    }
}