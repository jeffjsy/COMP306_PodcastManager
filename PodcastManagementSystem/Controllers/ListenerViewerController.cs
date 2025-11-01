using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PodcastManagementSystem.Interfaces;
using PodcastManagementSystem.Models;
using PodcastManagementSystem.Models.ViewModels;
namespace PodcastManagementSystem.Controllers
{

    public class ListenerViewerController : Controller
    {
        private readonly IPodcastRepository _podcastRepository;
        private readonly IEpisodeRepository _episodeRepository;
        private readonly ICommentRepository _commentRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public ListenerViewerController(
            IPodcastRepository podcastRepository, 
            ICommentRepository commentRepository, 
            IEpisodeRepository episodeRepository,
            UserManager<ApplicationUser> userManager)

        {
            _podcastRepository = podcastRepository;
            _commentRepository = commentRepository;
            _episodeRepository = episodeRepository;
            _userManager = userManager;
        }

        // ---------------------------------------------------------------------
        // 1. DISCOVER DASHBOARD (Public Listing)
        // ---------------------------------------------------------------------

        // GET: /ListenerViewer/Dashboard 
        public async Task<IActionResult> Dashboard()
        {
            // Action should retrieve all available podcasts
            var allPodcasts = await _podcastRepository.GetAllPodcastsAsync();

            // Pass the list of Podcast objects to the View
            return View(allPodcasts);
        }

        // ---------------------------------------------------------------------
        // 2. VIEW CHANNEL & EPISODES (Public Detail Page)
        // ---------------------------------------------------------------------

        // GET: /ListenerViewer/ViewChannel/5
        public async Task<IActionResult> ViewChannel(int podcastId)
        {
            // 1. Get the Podcast details
            var podcast = await _podcastRepository.GetPodcastByIdAsync(podcastId);

            if (podcast == null)
            {
                return NotFound();
            }

            // 2. Get the Episodes for that Podcast

            var episodes = await _podcastRepository.GetEpisodesByPodcastIdAsync(podcastId);

            // 3. Populate the ViewModel
            var viewModel = new ChannelDetailsViewModel
            {
                Channel = podcast,
                Episodes = episodes.ToList()
            };

            return View(viewModel); 
        }

        // GET: /ListenerViewer/ViewEpisode/{episodeId}
        public async Task<IActionResult> ViewEpisode(int episodeId)
        {
            // Fetch the single episode, including the Podcast (Channel) details
            var episode = await _episodeRepository.GetEpisodeByIdAsync(episodeId);

            if (episode == null)
            {
                TempData["Error"] = "Episode not found.";
                return RedirectToAction(nameof(Index)); // Or a suitable error page
            }

            // Fetch the comments for this episode, including the user details for display
            // Make sure your repository method includes ApplicationUser (User) data!
            var comments = await _commentRepository.GetCommentsByEpisodeIdAsync(episodeId);

            var model = new EpisodeCommentsViewModel
            {
                Episode = episode,
                EpisodeID = episodeId,
                Comments = comments.OrderBy(c => c.TimeStamp).ToList()
            };

            return View(model); // This renders the ViewEpisode.cshtml
        }

        // ---------------------------------------------------------------------
        // 2. ADD & EDIT COMMENTS 
        // ---------------------------------------------------------------------

        [HttpPost]
        [Authorize] // Only logged-in users can post comments
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int episodeId, string commentText)
        {
            // 1. INPUT VALIDATION
            if (string.IsNullOrWhiteSpace(commentText) || episodeId == 0)
            {
                TempData["Error"] = "Comment text cannot be empty.";
                return RedirectToAction(nameof(ViewEpisode), new { episodeId = episodeId });
            }

            // 2. Get the current logged-in user's ID and convert it to Guid
            var userIdString = _userManager.GetUserId(User);

            // Safety check for Guid parsing, though Authorize should ensure ID exists
            if (userIdString == null || !Guid.TryParse(userIdString, out Guid userGuid))
            {
                TempData["Error"] = "User identity could not be resolved.";
                return RedirectToAction(nameof(ViewEpisode), new { episodeId = episodeId });
            }

            // 3. Create the new Comment entity
            var comment = new Comment
            {
                EpisodeID = episodeId,
                UserID = userGuid, 
                Text = commentText.Trim(), // Trim whitespace
                TimeStamp = DateTime.UtcNow // Use UTC for consistency
            };

            // 4. Save the comment
            try
            {
                await _commentRepository.AddCommentAsync(comment);
                TempData["SuccessMessage"] = "Your comment has been posted.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to post comment due to a database error.";
            }


            //Redirect to the episode page to reload and display the new comment
            return RedirectToAction(nameof(ViewEpisode), new { episodeId = episodeId });
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditComment(int commentId, int episodeId, string updatedText)
        {
            // 1. Input and Authorization Validation
            if (string.IsNullOrWhiteSpace(updatedText))
            {
                TempData["Error"] = "Comment text cannot be empty.";
                return RedirectToAction(nameof(ViewEpisode), new { episodeId = episodeId });
            }

            // Ensure the user owns the comment and it's within the 24-hour window
            var comment = await _commentRepository.GetCommentByIdAsync(commentId);
            var userIdString = _userManager.GetUserId(User);
            Guid currentUserIdGuid;

            if (comment == null)
            {
                TempData["Error"] = "Comment not found.";
                return RedirectToAction(nameof(ViewEpisode), new { episodeId = episodeId });
            }

            // Check if the current user is the comment owner AND the 24-hour limit has not passed
            if (!Guid.TryParse(userIdString, out currentUserIdGuid) || comment.UserID != currentUserIdGuid || (DateTime.UtcNow - comment.TimeStamp).TotalHours > 24)
            {
                TempData["Error"] = "You are not authorized to edit this comment or the edit window has closed.";
                return RedirectToAction(nameof(ViewEpisode), new { episodeId = episodeId });
            }

            // 2. Update the Comment
            comment.Text = updatedText.Trim();
            comment.TimeStamp = DateTime.UtcNow; 

            // 3. Save Changes
            try
            {
                await _commentRepository.UpdateCommentAsync(comment); 
                TempData["SuccessMessage"] = "Comment updated successfully!";
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to update comment due to a server error.";
            }

            // 4. Redirect
            return RedirectToAction(nameof(ViewEpisode), new { episodeId = episodeId });
        }
    }
}