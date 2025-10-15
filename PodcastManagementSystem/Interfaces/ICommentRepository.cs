using PodcastManagementSystem.Models;

namespace PodcastManagementSystem.Interfaces
{
    public interface ICommentRepository
    {
        // 1. Add comments (Lab Requirement)
        Task AddCommentAsync(Comment comment);

        // 2. List all comments about a specific episode (Lab Requirement)
        Task<List<Comment>> GetCommentsByEpisodeAsync(int episodeId);

        // 3. Modify a comment (Lab Requirement - requires checking timestamp and UserID)
        Task<bool> UpdateCommentAsync(Comment comment);

        // Helper to get a single comment for update/verification
        Task<Comment> GetCommentByIdAsync(string commentId);
    }
}
