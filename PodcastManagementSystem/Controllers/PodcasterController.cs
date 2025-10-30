using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PodcastManagementSystem.Interfaces;
using PodcastManagementSystem.Models;
using PodcastManagementSystem.Models.ViewModels;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace PodcastManagementSystem.Controllers
{
    // 🛡️ SECURITY: Only users assigned the "Podcaster" role can access this controller.
    [Authorize(Roles = "Podcaster")]
    public class PodcasterController : Controller
    {
        private readonly IPodcastRepository _podcastRepository;
        private readonly IEpisodeRepository _episodeRepository;
        private readonly IS3Service _s3Service;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PodcasterController> _logger;

        public PodcasterController(
            IPodcastRepository podcastRepository,
            IEpisodeRepository episodeRepository,
            IS3Service s3Service,
            UserManager<ApplicationUser> userManager,
            ILogger<PodcasterController> logger)
        {
            _podcastRepository = podcastRepository;
            _episodeRepository = episodeRepository;
            _s3Service = s3Service;
            _userManager = userManager;
            _logger = logger;
        }

        // ---------------------------------------------------------------------
        // 1. PODCAST DASHBOARD (READ)
        // ---------------------------------------------------------------------
        // GET: Podcaster/Dashboard - Lists all podcasts created by the current user
        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            var creatorIdGuid = Guid.Parse(userId);

            // Retrieve podcasts using the new repository method
            var myPodcasts = await _podcastRepository.GetPodcastsByCreatorIdAsync(creatorIdGuid);

            return View(myPodcasts);
        }

        // ---------------------------------------------------------------------
        // 2. CREATE / MANAGE PODCAST (CRUD)
        // ---------------------------------------------------------------------

        // GET: Podcaster/CreatePodcast
        public IActionResult CreatePodcast()
        {
            return View(new PodcastViewModel());
        }

        // POST: Podcaster/CreatePodcast
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePodcast(PodcastViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var podcast = new Podcast
            {
                Title = model.Title,
                Description = model.Description,
                CreatorID = Guid.Parse(userId),
                CreatedDate = DateTime.UtcNow
            };

            await _podcastRepository.AddPodcastAsync(podcast);

            return RedirectToAction(nameof(Dashboard));

        }

        // GET: Podcaster/ManagePodcast/5
        public async Task<IActionResult> ManagePodcast(int podcastId)
        {
            // 1. Get the current user's ID (a string)
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2. Convert the string userId to a Guid
            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                return Forbid();
            }

            // 3. Get the specific podcast channel
            var podcast = await _podcastRepository.GetPodcastByIdAsync(podcastId);

            // Security Check: Ensure the podcast exists AND belongs to the current Podcaster
            if (podcast == null || podcast.CreatorID != userId)
            {
                return NotFound();
            }

            // 4. Get all episodes for that channel (using IPodcastRepository method)
            var episodes = await _podcastRepository.GetEpisodesByPodcastIdAsync(podcastId);

            // 5. Populate the ViewModel
            var viewModel = new ChannelDetailsViewModel
            {
                Channel = podcast,
                Episodes = episodes.ToList()
            };

            return View(viewModel);
        }

        // ---------------------------------------------------------------------
        // 3. EPISODE MANAGEMENT (UPLOAD)
        // ---------------------------------------------------------------------

        // GET: Podcaster/AddEpisode/{podcastId}
        public async Task<IActionResult> AddEpisode(int podcastId)
        {
            var podcast = await _podcastRepository.GetPodcastByIdAsync(podcastId);
            var currentUserId = _userManager.GetUserId(User);

            // Security check: Verify ownership
            if (podcast == null || podcast.CreatorID != Guid.Parse(currentUserId))
            {
                return NotFound();
            }

            var viewModel = new AddEpisodeViewModel { PodcastID = podcastId };
            ViewData["PodcastTitle"] = podcast.Title; 
            return View(viewModel);
        }

        // POST: Podcaster/AddEpisode
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(200_000_000)]
        public async Task<IActionResult> AddEpisode(AddEpisodeViewModel model)
        {
            //  Check validation failure
            if (!ModelState.IsValid || model.AudioFile == null)
            {
                _logger.LogWarning("Episode upload failed validation for PodcastID: {PodcastId}. Errors: {@Errors}",
                    model.PodcastID, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));

                var podcast = await _podcastRepository.GetPodcastByIdAsync(model.PodcastID);
                ViewData["PodcastTitle"] = podcast?.Title;

                // If this returns, check validation messages in the browser
                return View(model);
            }

            // 🌟 LOG POINT 2: Before S3 Upload
            _logger.LogInformation("Validation passed. Attempting S3 upload for episode '{Title}' on podcast {Id}.",
                model.Title, model.PodcastID);

            // 1. Upload the file to S3
            var fileKey = $"episodes/{model.PodcastID}/{Guid.NewGuid()}-{model.AudioFile.FileName}";
            string audioFileUrl = null;

            // Use a try-catch block to specifically log S3 exceptions
            try
            {
                audioFileUrl = await _s3Service.UploadFileAsync(model.AudioFile, fileKey);
            }
            catch (Exception ex)
            {
                // 🛑 CRITICAL LOGGING: Logs the full exception details
                _logger.LogError(ex, "S3 Upload FAILED for file key {Key}. Check AWS credentials and bucket configuration.", fileKey);
                ModelState.AddModelError("", "S3 service error: Could not upload file. Check server logs for details.");

                var podcast = await _podcastRepository.GetPodcastByIdAsync(model.PodcastID);
                ViewData["PodcastTitle"] = podcast?.Title;
                return View(model);
            }

            // 🌟 LOG POINT 3: Check S3 URL result
            if (string.IsNullOrEmpty(audioFileUrl))
            {
                _logger.LogError("S3 service returned a NULL or empty URL for file key {Key}. Check S3 service return logic.", fileKey);
                ModelState.AddModelError("", "Storage error: File upload resulted in an invalid URL.");

                var podcast = await _podcastRepository.GetPodcastByIdAsync(model.PodcastID);
                ViewData["PodcastTitle"] = podcast?.Title;
                return View(model);
            }

            // 🌟 LOG POINT 4: Before Repository Save
            _logger.LogInformation("S3 upload successful (URL: {Url}). Saving episode metadata to database.", audioFileUrl);


            // 2. Save metadata to the database
            var episode = new Episode
            {
                PodcastID = model.PodcastID,
                Title = model.Title,
                ReleaseDate = DateTime.UtcNow,
                AudioFileURL = audioFileUrl,
                DurationMinutes = model.DurationMinutes
            };

            // 3. Save the episode using the IEpisodeRepository
            await _episodeRepository.AddEpisodeAsync(episode);

            // 🌟 LOG POINT 5: Success
            _logger.LogInformation("Episode '{Title}' successfully published and saved.", model.Title);

            // 4. Redirect to the episode listing (ManagePodcast)
            TempData["SuccessMessage"] = $"Episode '{episode.Title}' published successfully!";
            return RedirectToAction(nameof(ManagePodcast), new { podcastId = model.PodcastID });
        }

        // ---------------------------------------------------------------------
        // 4. EPISODE DELETION (DELETE)
        // ---------------------------------------------------------------------

        // POST: Podcaster/DeleteEpisode/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Podcaster")]
        public async Task<IActionResult> DeleteEpisode(int episodeId)
        {
            // 1. Get the episode to find its parent podcast ID
            var episodeToDelete = await _episodeRepository.GetEpisodeByIdAsync(episodeId);

            if (episodeToDelete == null)
            {
                _logger.LogWarning("Attempted to delete non-existent episode with ID: {EpisodeId}", episodeId);
                TempData["ErrorMessage"] = "Episode not found.";

                // Fall back to the dashboard if we can't determine the podcast ID
                return RedirectToAction(nameof(Dashboard));
            }

            var podcastId = episodeToDelete.PodcastID;

            // Security Check: Verify that the current user owns the podcast/episode
            var podcast = await _podcastRepository.GetPodcastByIdAsync(podcastId);
            var currentUserId = _userManager.GetUserId(User);

            if (podcast == null || podcast.CreatorID != Guid.Parse(currentUserId))
            {
                _logger.LogWarning("User {UserId} attempted to delete episode {EpisodeId} from unowned podcast {PodcastId}", currentUserId, episodeId, podcastId);
                return Forbid(); // User does not own this content
            }

            // 2. IMPORTANT: Delete the file from S3 first!
            var fileKeyToDelete = episodeToDelete.AudioFileURL.Split('/').Last();

            try
            {
                await _s3Service.DeleteFileAsync(fileKeyToDelete);
                _logger.LogInformation("Successfully deleted S3 file with key: {Key}", fileKeyToDelete);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete S3 file with key: {Key}. Proceeding with DB deletion.", fileKeyToDelete);
                // We typically continue with DB deletion even if S3 fails, as the link is broken anyway.
            }

            // 3. Delete metadata from the database
            await _episodeRepository.DeleteEpisodeAsync(episodeId);

            TempData["SuccessMessage"] = $"Episode '{episodeToDelete.Title}' deleted successfully, and file removed from storage.";

            // 4. Redirect back to the episode management view
            return RedirectToAction(nameof(ManagePodcast), new { podcastId = podcastId });
        }
    }
}