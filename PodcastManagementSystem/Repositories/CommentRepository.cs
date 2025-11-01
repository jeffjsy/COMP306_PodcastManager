using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PodcastManagementSystem.Data;
using PodcastManagementSystem.Interfaces;
using PodcastManagementSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PodcastManagementSystem.Repositories
{
    public class CommentRepository : ICommentRepository
    {
        private readonly ApplicationDbContext _context;

        public CommentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Add comments
        public async Task AddCommentAsync(Comment comment)
        {
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
        }

        // 2. List comments (Eager load the User for display)
        public async Task<IEnumerable<Comment>> GetCommentsByEpisodeIdAsync(int episodeId)
        {
            return await _context.Comments
                .Include(c => c.User) // Necessary to display the username with the comment
                .Where(c => c.EpisodeID == episodeId)
                .OrderByDescending(c => c.TimeStamp) // Newest first
                .ToListAsync();
        }

        // 3. Get comment by ID for security/edit check
        public async Task<Comment> GetCommentByIdAsync(int commentId)
        {
            return await _context.Comments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CommentID == commentId);
        }

        // 3. Update comments (Uses the critical update technique)
        public async Task UpdateCommentAsync(Comment comment)
        {
            _context.Entry(comment).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        //  Delete comments
        public async Task DeleteCommentAsync(Comment comment)
        {
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
        }
    }
}