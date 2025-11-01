using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PodcastManagementSystem.Models;

namespace PodcastManagementSystem.Models.ViewModels
{
    public class EpisodeCommentsViewModel
    {
        // Data for the specific episode being viewed
        public Episode Episode { get; set; }

        // List of existing comments for that episode
        public IEnumerable<Comment> Comments { get; set; } = new List<Comment>();
        
        // Comment to be added to Episode
        public string NewCommentContent { get; set; }

        // Hidden field to capture the EpisodeID when the form is submitted
        public int EpisodeID { get; set; }
    }
}