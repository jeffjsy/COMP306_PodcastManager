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

            // Note: AddEpisodeViewModel MUST include a [Required] Description property now.
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
            
            if (!ModelState.IsValid || model.AudioFile == null)
            {
                _logger.LogWarning("Episode upload failed validation for PodcastID: {PodcastId}. Errors: {@Errors}",
                    model.PodcastID, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));

                var podcast = await _podcastRepository.GetPodcastByIdAsync(model.PodcastID);
                ViewData["PodcastTitle"] = podcast?.Title;

                // If this returns, check validation messages in the browser
                return View(model);
            }

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
                _logger.LogError(ex, "S3 Upload FAILED for file key {Key}. Check AWS credentials and bucket configuration.", fileKey);
                ModelState.AddModelError("", "S3 service error: Could not upload file. Check server logs for details.");

                var podcast = await _podcastRepository.GetPodcastByIdAsync(model.PodcastID);
                ViewData["PodcastTitle"] = podcast?.Title;
                return View(model);
            }

            if (string.IsNullOrEmpty(audioFileUrl))
            {
                _logger.LogError("S3 service returned a NULL or empty URL for file key {Key}. Check S3 service return logic.", fileKey);
                ModelState.AddModelError("", "Storage error: File upload resulted in an invalid URL.");

                var podcast = await _podcastRepository.GetPodcastByIdAsync(model.PodcastID);
                ViewData["PodcastTitle"] = podcast?.Title;
                return View(model);
            }

            _logger.LogInformation("S3 upload successful (URL: {Url}). Saving episode metadata to database.", audioFileUrl);


            // 2. Save metadata to the database
            var episode = new Episode
            {
                PodcastID = model.PodcastID,
                Title = model.Title,
                Description = model.Description,
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
        // 3.5. UPDATE EPISODE  
        // ---------------------------------------------------------------------

        // POST: /Podcaster/EditEpisode
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEpisode(Episode model)
        {
            if (!ModelState.IsValid)
            {
                // This will likely return to the GET EditEpisode view with errors
                return View(model);
            }

            _logger.LogInformation("TRACE 1: Edit attempt received for Episode {ID}. New Title: {Title}", model.EpisodeID, model.Title);

            // 1. Get the original episode object, which is currently tracked by the context.
            var episodeToUpdate = await _episodeRepository.GetEpisodeByIdAsync(model.EpisodeID);
            if (episodeToUpdate == null) return NotFound();

            // 2. Update ONLY the editable properties (Title, Description, Duration, etc.)
            episodeToUpdate.Title = model.Title;
            episodeToUpdate.Description = model.Description;
            episodeToUpdate.DurationMinutes = model.DurationMinutes;


            try
            {
                _logger.LogInformation("TRACE 2: Calling repository to update Episode {ID}.", model.EpisodeID);
                await _episodeRepository.UpdateEpisodeAsync(episodeToUpdate);


                _logger.LogInformation("TRACE 3: Repository call completed successfully for Episode {ID}.", model.EpisodeID);
                TempData["SuccessMessage"] = $"Episode '{episodeToUpdate.Title}' updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TRACE ABNORMAL: Saving failed for Episode {ID}.", model.EpisodeID);
                ModelState.AddModelError("", "An error occurred while saving changes.");
                return View(model);
            }

            _logger.LogInformation("TRACE 4: Redirecting to ManagePodcast for Episode {ID}.", model.EpisodeID);
            // 3. Redirect back to the episode management page
            return RedirectToAction(nameof(ManagePodcast), new { podcastId = episodeToUpdate.PodcastID });
        }

        public async Task<IActionResult> EditEpisode(int? id)
        {
            if (id == null) return NotFound();

            // Fetch the episode from the repository
            var episode = await _episodeRepository.GetEpisodeByIdAsync(id.Value);

            if (episode == null) return NotFound();

            return View(episode);
        }


        // ---------------------------------------------------------------------
        // 4. EPISODE DELETION (DELETE)
        // ---------------------------------------------------------------------

        // GET: /Podcaster/DeleteEpisode/3 (Confirmation Page)
        public async Task<IActionResult> DeleteEpisode(int? id)
        {
            if (id == null) return NotFound();

            var episode = await _episodeRepository.GetEpisodeByIdAsync(id.Value);
            if (episode == null)
            {
                // This is where you got the 404 before!
                return NotFound();
            }

            return View(episode);
        }

        // POST: /Podcaster/DeleteEpisode/3 (Execution)
        [HttpPost, ActionName("DeleteEpisode")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEpisodeConfirmed(int id)
        {
            // Get Podcast ID BEFORE deleting the episode record!
            int podcastId = await _episodeRepository.GetPodcastIdForEpisodeAsync(id);

            await _episodeRepository.DeleteEpisodeAsync(id);

            if (podcastId > 0)
            {
                // Redirect back to the managing page for the parent podcast
                return RedirectToAction("ManagePodcast", new { podcastId = podcastId });
            }
            return RedirectToAction("Index", "Home");
        }

        // ---------------------------------------------------------------------
        // 5. DELETE PODCAST
        // ---------------------------------------------------------------------

        // GET: Podcaster/Delete/{podcastId}
        // Loads deletaion confirmation page
        public async Task<IActionResult> Delete(int podcastId)
        {
            var podcast = await _podcastRepository.GetPodcastByIdAsync(podcastId);

            if (podcast == null)
            {
                return NotFound();
            }

            // Only allow the creator to delete their own podcast
            var currentUserId = Guid.Parse(_userManager.GetUserId(User));
            if (podcast.CreatorID != currentUserId)
            {
                return Forbid();
            }

            return View(podcast); // Passes the podcast to Delete.cshtml
        }


        // POST: Podcaster/DeleteConfirmed/{podcastId}
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int podcastId)
        {
            var podcast = await _podcastRepository.GetPodcastByIdAsync(podcastId);
            if (podcast == null || podcast.CreatorID != Guid.Parse(_userManager.GetUserId(User)))
                return NotFound();

            await _podcastRepository.DeletePodcastAsync(podcastId);

            return RedirectToAction(nameof(Dashboard));
        }
    }
}