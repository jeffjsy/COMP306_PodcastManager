using PodcastManagementSystem.Models;

namespace PodcastManagementSystem.Interfaces
{
    public interface ICommentRepository
    {
        Task AddCommentAsync(Comment comment);
        Task<List<Comment>> GetCommentsByEpisodeIdAsync(int episodeId);
        Task UpdateCommentAsync(Comment comment);
        Task<Comment> GetCommentByIdAsync(int episodeId, Guid commentId); 
        Task DeleteCommentAsync(Comment comment);



    }
}
