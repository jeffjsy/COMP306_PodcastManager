using PodcastManagementSystem.Models;

namespace PodcastManagementSystem.Interfaces
{
    public interface ICommentRepository
    {
        // 1. Add comments (Lab Requirement)
        Task AddCommentAsync(Comment comment);

        // 2. List all comments about a specific episode 
        Task<IEnumerable<Comment>> GetCommentsByEpisodeIdAsync(int episodeId);

        // 3. Modify comments 
        Task<Comment> GetCommentByIdAsync(int commentId);
        Task UpdateCommentAsync(Comment comment);
        Task DeleteCommentAsync(int commentId);



    }
}
