using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PodcastManagementSystem.Interfaces;
using PodcastManagementSystem.Models;
using System.Linq;
using System.Threading.Tasks;

namespace PodcastManagementSystem.Controllers
{
    [Authorize(Roles = "Listener/viewer, Admin, Podcaster")] // Restrict access to logged-in users
    public class ListenerController : Controller
    {
        private readonly IPodcastRepository _podcastRepo;
        private readonly ICommentRepository _commentRepo;
        private readonly UserManager<IdentityUser> _userManager;

        public ListenerController(IPodcastRepository podcastRepo, ICommentRepository commentRepo, UserManager<IdentityUser> userManager)
        {
            _podcastRepo = podcastRepo;
            _commentRepo = commentRepo;
            _userManager = userManager;
        }

        // GET: /Listener/Dashboard
        public async Task<IActionResult> Dashboard(string searchString, string creatorId, string viewType = "all")
        {
            // View logic based on selection: Popular, Recent, or All
            if (!string.IsNullOrWhiteSpace(searchString) || !string.IsNullOrWhiteSpace(creatorId))
            {
                var searchResults = await _podcastRepo.SearchEpisodesAsync(searchString, creatorId);
                ViewBag.SearchTerm = searchString;
                return View(searchResults);
            }

            switch (viewType.ToLower())
            {
                case "popular":
                    return View(await _podcastRepo.GetPopularEpisodesAsync());
                case "recent":
                    return View(await _podcastRepo.GetRecentEpisodesAsync());
                case "all":
                default:
                    // Show a mix or all podcasts/episodes on the default dashboard view
                    return View(await _podcastRepo.GetRecentEpisodesAsync());
            }
        }

        // GET: /Listener/Episode/5
        public async Task<IActionResult> Episode(int id)
        {
            var episode = await _podcastRepo.GetEpisodeDetailsAsync(id);
            if (episode == null) return NotFound();

            // 1. Increment PlayCount (RDBMS update)
            await _podcastRepo.IncrementPlayCountAsync(id);

            // 2. Fetch Comments (DynamoDB read)
            var comments = await _commentRepo.GetCommentsByEpisodeIdAsync(id);

            // Use a ViewModel to hold complex data if needed, but for now, use ViewBag/Tuple.
            ViewBag.Comments = comments;
            ViewBag.IsSubscribed = await _podcastRepo.IsSubscribedAsync(_userManager.GetUserId(User), episode.PodcastID);

            return View(episode);
        }

        // POST: /Listener/Subscribe/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Subscribe(int podcastId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var success = await _podcastRepo.AddSubscriptionAsync(userId, podcastId);

            if (success)
            {
                TempData["Message"] = "Successfully subscribed to the podcast!";
            }
            else
            {
                TempData["Error"] = "You are already subscribed to this podcast.";
            }

            // Redirect back to the podcast or episode page
            return RedirectToAction(nameof(Episode), new { id = TempData["EpisodeId"] });
        }

        // POST: /Listener/AddComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int episodeId, string commentText)
        {
            if (string.IsNullOrWhiteSpace(commentText))
            {
                TempData["Error"] = "Comment cannot be empty.";
                return RedirectToAction(nameof(Episode), new { id = episodeId });
            }

            var comment = new Comment
            {
                CommentID = System.Guid.NewGuid().ToString(),
                EpisodeID = episodeId,
                UserID = _userManager.GetUserId(User),
                Text = commentText,
                Timestamp = System.DateTime.UtcNow
            };

            await _commentRepo.AddCommentAsync(comment);
            TempData["Message"] = "Comment added successfully.";
            return RedirectToAction(nameof(Episode), new { id = episodeId });
        }

        // POST: /Listener/EditComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditComment(string commentId, int episodeId, string updatedText)
        {
            var existingComment = await _commentRepo.GetCommentByIdAsync(commentId, episodeId);

            if (existingComment == null || existingComment.UserID != _userManager.GetUserId(User))
            {
                TempData["Error"] = "You are not authorized to edit this comment.";
                return RedirectToAction(nameof(Episode), new { id = episodeId });
            }

            // --- MANDATORY RUBRIC LOGIC: 24-HOUR RESTRICTION ---
            if (existingComment.Timestamp < System.DateTime.UtcNow.AddHours(-24))
            {
                TempData["Error"] = "Comments can only be edited within 24 hours of posting.";
                return RedirectToAction(nameof(Episode), new { id = episodeId });
            }
            // ----------------------------------------------------

            existingComment.Text = updatedText;
            existingComment.Timestamp = System.DateTime.UtcNow; // Update timestamp on edit
            await _commentRepo.UpdateCommentAsync(existingComment);

            TempData["Message"] = "Comment updated successfully.";
            return RedirectToAction(nameof(Episode), new { id = episodeId });
        }

    }
}
