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
        private readonly IAnalyticsRepository _analyticsRepository;

        public ListenerViewerController(
            IPodcastRepository podcastRepository, 
            ICommentRepository commentRepository, 
            IEpisodeRepository episodeRepository,
            UserManager<ApplicationUser> userManager,
            IAnalyticsRepository analyticsRepository)

        {
            _podcastRepository = podcastRepository;
            _commentRepository = commentRepository;
            _episodeRepository = episodeRepository;
            _userManager = userManager;
            _analyticsRepository = analyticsRepository;
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

        // File Location: Controllers/ListenerViewerController.cs
        public async Task<IActionResult> ViewEpisode(int episodeId)
        {
            // Fetch the single episode, including the Podcast (Channel) details
            var episode = await _episodeRepository.GetEpisodeByIdAsync(episodeId);

            if (episode == null)
            {
                TempData["Error"] = "Episode not found.";
                return RedirectToAction(nameof(Index)); // Or a suitable error page
            }

            // Analytics 
            Guid? userId = null;
            if (User.Identity.IsAuthenticated)
            {
                userId = Guid.Parse(_userManager.GetUserId(User));
            }
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            _ = _analyticsRepository.RecordViewAsync(episodeId, userId, ipAddress);
            await _analyticsRepository.LogEpisodeViewAsync(episode.PodcastID, episode.EpisodeID);

            // 1. Fetch the comments for this episode from DynamoDB
            var comments = await _commentRepository.GetCommentsByEpisodeIdAsync(episodeId);

            // 2. 🌟 NEW: Manually fetch ApplicationUser details for each comment 🌟
            // DynamoDB doesn't automatically join with the SQL Identity database.
            if (comments != null)
            {
                foreach (var comment in comments)
                {
                    comment.User = await _userManager.FindByIdAsync(comment.UserID.ToString());

                    // Handle case where user might be deleted (optional)
                    if (comment.User == null)
                    {
                        // Set a default or anonymous user placeholder if the user doesn't exist
                        comment.User = new ApplicationUser { UserName = "[Deleted User]" };
                    }
                }
            }

            // 3. Prepare and return the View Model
            var model = new EpisodeCommentsViewModel
            {
                Episode = episode,
                EpisodeID = episodeId,
                Comments = comments?.OrderBy(c => c.TimeStamp).ToList() ?? new List<Comment>()
            };

            return View(model); // This renders the ViewEpisode.cshtml
        }

        // ---------------------------------------------------------------------
        // 3. ADD & EDIT & DELETE COMMENTS
        // ---------------------------------------------------------------------

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int episodeId, string commentText)
        {
            // 1. INPUT VALIDATION
            if (string.IsNullOrWhiteSpace(commentText) || episodeId == 0)
            {
                TempData["Error"] = "Comment text cannot be empty.";
                return RedirectToAction(nameof(ViewEpisode), new { episodeId = episodeId });
            }

            // Get episode data to find the PodcastID
            var episode = await _episodeRepository.GetEpisodeByIdAsync(episodeId);
            if (episode == null)
            {
                TempData["Error"] = "Episode not found.";
                return RedirectToAction(nameof(Index)); 
            }
            int podcastId = episode.PodcastID;

            // 2. Get the current logged-in user's ID and convert it to Guid
            var userIdString = _userManager.GetUserId(User);
            if (userIdString == null || !Guid.TryParse(userIdString, out Guid userGuid))
            {
                TempData["Error"] = "User identity could not be resolved.";
                return RedirectToAction(nameof(ViewEpisode), new { episodeId = episodeId });
            }

            // 3. Create the new Comment entity
            var comment = new Comment
            {
                CommentID = Guid.NewGuid(),
                EpisodeID = episodeId,
                PodcastID = podcastId, 
                UserID = userGuid,
                Text = commentText.Trim(),
                TimeStamp = DateTime.UtcNow
            };

            // 4. Save the comment and update analytics
            try
            {
                await _commentRepository.AddCommentAsync(comment);

                // Update analytics page with comment count
                await _analyticsRepository.UpdateCommentCountAsync(podcastId, episodeId, 1);

                TempData["SuccessMessage"] = "Your comment has been posted.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to post comment due to a database error.";
            }

            //Redirect to the episode page to reload and display the new comment
            return RedirectToAction(nameof(ViewEpisode), new { episodeId = episodeId });
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        // 🌟 CHANGE 1: Accept commentId as a string to handle the full GUID value 🌟
        public async Task<IActionResult> EditComment(string commentId, int episodeId, string updatedText)
        {
            // 1. Input and Authorization Validation
            if (string.IsNullOrWhiteSpace(updatedText))
            {
                TempData["Error"] = "Comment text cannot be empty.";
                return RedirectToAction(nameof(ViewEpisode), new { episodeId = episodeId });
            }

            // 🌟 CHANGE 2: Safely parse the commentId string into a Guid 🌟
            if (!Guid.TryParse(commentId, out Guid commentGuid))
            {
                TempData["Error"] = "Invalid comment identifier format.";
                return RedirectToAction(nameof(ViewEpisode), new { episodeId = episodeId });
            }

            // Ensure the user owns the comment and it's within the 24-hour window
            // 🌟 CHANGE 3: Use the correctly parsed Guid for the repository lookup 🌟
            var comment = await _commentRepository.GetCommentByIdAsync(episodeId, commentGuid);

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

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(string commentId, int episodeId)
        {
            // 1. Input Validation and Parsing
            if (!Guid.TryParse(commentId, out Guid commentGuid))
            {
                TempData["Error"] = "Invalid comment ID format.";
                return RedirectToAction(nameof(ViewEpisode), new { episodeId });
            }

            var comment = await _commentRepository.GetCommentByIdAsync(episodeId, commentGuid);
            var userIdString = _userManager.GetUserId(User);
            Guid currentUserIdGuid;

            if (comment == null)
            {
                TempData["Error"] = "Comment not found.";
                return RedirectToAction(nameof(ViewEpisode), new { episodeId = episodeId });
            }

            // 2. Authorization Check: Ensure the current user is the owner
            if (!Guid.TryParse(userIdString, out currentUserIdGuid) || comment.UserID != currentUserIdGuid)
            {
                TempData["Error"] = "You are not authorized to delete this comment.";
                return RedirectToAction(nameof(ViewEpisode), new { episodeId = episodeId });
            }

            // 3. Delete the Comment and update analytics
            try
            {
                await _commentRepository.DeleteCommentAsync(comment);

                // Update Analytics page by removing comment count
                await _analyticsRepository.UpdateCommentCountAsync(comment.PodcastID, episodeId, -1);

                TempData["SuccessMessage"] = "Comment deleted successfully.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to delete comment due to a server error.";
            }

            return RedirectToAction(nameof(ViewEpisode), new { episodeId = episodeId });
        }

        // ---------------------------------------------------------------------
        // 4. ANALYTICS
        // ---------------------------------------------------------------------

        [Authorize(Roles = "Podcaster, Admin")] // Restrict access
        public async Task<IActionResult> EpisodeStats(int podcastId) // Pass the podcastId to report on
        {
            // 1. Get Summary Data from DynamoDB
            var summaries = await _analyticsRepository.GetEpisodeSummariesByPodcastIdAsync(podcastId);

            // 2. Fetch RDBMS data (Episode Titles, Podcast Title)
            // You'll need a way to get the Podcast Title and the Episode Titles based on the IDs
            var podcast = await _podcastRepository.GetPodcastByIdAsync(podcastId);
            var allEpisodes = await _episodeRepository.GetEpisodesByPodcastIdAsync(podcastId);

            var episodeMap = allEpisodes.ToDictionary(e => e.EpisodeID, e => e.Title);

            // 3. Populate RDBMS data into the DynamoDB objects and sort
            foreach (var summary in summaries)
            {
                summary.PodcastTitle = podcast.Title;
                if (episodeMap.ContainsKey(summary.EpisodeID))
                {
                    summary.EpisodeTitle = episodeMap[summary.EpisodeID];
                }
            }

            // 4. Sort and select Top 10 (or all)
            var topEpisodes = summaries
                                .OrderByDescending(s => s.ViewCount) // Sort by the aggregated metric
                                .Take(10)
                                .ToList();

            var model = new EpisodeStatsViewModel
            {
                PodcastTitle = podcast.Title,
                TopEpisodes = topEpisodes
            };

            return View(model);
        }
    }
}