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
    [Authorize(Roles = "Podcaster")]
    public class PodcasterController : Controller
    {
        private readonly IPodcastRepository _podcastRepository;
        private readonly IEpisodeRepository _episodeRepository;
        private readonly IS3Service _s3Service;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PodcasterController> _logger;
        private readonly IAnalyticsRepository _analyticsRepository;

        public PodcasterController(
            IPodcastRepository podcastRepository,
            IEpisodeRepository episodeRepository,
            IS3Service s3Service,
            UserManager<ApplicationUser> userManager,
            ILogger<PodcasterController> logger,
            IAnalyticsRepository analyticsRepository)

        {
            _podcastRepository = podcastRepository;
            _episodeRepository = episodeRepository;
            _s3Service = s3Service;
            _userManager = userManager;
            _logger = logger;
            _analyticsRepository = analyticsRepository;
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
                var podcast = await _podcastRepository.GetPodcastByIdAsync(model.PodcastID);
                ViewData["PodcastTitle"] = podcast?.Title;
                return View(model);
            }

            try
            {
                var episode = await _episodeRepository.AddEpisodeAsync(model);

                _logger.LogInformation("Episode '{Title}' successfully published.", episode.Title);
                TempData["SuccessMessage"] = $"Episode '{episode.Title}' published successfully!";
                return RedirectToAction(nameof(ManagePodcast), new { podcastId = model.PodcastID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add episode for PodcastID {PodcastId}.", model.PodcastID);

                var podcast = await _podcastRepository.GetPodcastByIdAsync(model.PodcastID);
                ViewData["PodcastTitle"] = podcast?.Title;
                ModelState.AddModelError("", "An error occurred while uploading the episode. Please try again.");
                return View(model);
            }
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
                return NotFound();
            }

            return View(episode);
        }

        // POST: /Podcaster/DeleteEpisode/3 (Execution)
        [HttpPost, ActionName("DeleteEpisode")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEpisodeConfirmed(int id)
        {
            // Get the episode BEFORE deleting the record to access PodcastID and AudioFileURL
            var episodeToDelete = await _episodeRepository.GetEpisodeByIdAsync(id);
            if (episodeToDelete == null)
            {
                TempData["Error"] = "Episode not found.";
                return RedirectToAction(nameof(Dashboard));
            }

            int podcastId = episodeToDelete.PodcastID;

            try
            {
                // 1. Delete the episode from the RDBMS
                await _episodeRepository.DeleteEpisodeByIdAsync(id);

                // 2. Delete the file from S3 (assuming _s3Service has a delete method)
                // This is an essential step for full cleanup
                // await _s3Service.DeleteFileAsync(episodeToDelete.AudioFileURL); 

                // 🌟 MODIFICATION: Delete the corresponding analytics entry from DynamoDB 🌟
                await _analyticsRepository.DeleteEpisodeSummaryAsync(podcastId, id);

                TempData["SuccessMessage"] = $"Episode '{episodeToDelete.Title}' and its analytics successfully deleted.";

                // 3. Redirect back to the managing page for the parent podcast
                return RedirectToAction("ManagePodcast", new { podcastId = podcastId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete episode {EpisodeId} and/or its associated data.", id);
                TempData["Error"] = "Failed to complete deletion due to a server error.";
                return RedirectToAction("ManagePodcast", new { podcastId = podcastId });
            }
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

            try
            {
                // 1. Delete all associated episodes (RDBMS, S3, and Analytics)
                var episodes = await _episodeRepository.GetEpisodesByPodcastIdAsync(podcastId);

                // Manual analytics cleanup is safer if the repository doesn't handle it
                foreach (var episode in episodes)
                {
                    await _analyticsRepository.DeleteEpisodeSummaryAsync(podcastId, episode.EpisodeID);
                }

                // Delete all episodes (SQL records and S3 files)
                await _episodeRepository.DeleteAllEpisodesByPodcastIdAsync(podcast.PodcastID);


                // 2. Delete the main podcast record (including subscriptions)
                await _podcastRepository.DeletePodcastAsync(podcastId); // deletes subs then podcast

                TempData["SuccessMessage"] = $"Podcast '{podcast.Title}' and all associated episodes were deleted.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete podcast {PodcastId} completely.", podcastId);
                TempData["Error"] = "Failed to delete the podcast completely due to a server error.";
                return RedirectToAction(nameof(ManagePodcast), new { podcastId });
            }

            return RedirectToAction(nameof(Dashboard));
        }

        // ---------------------------------------------------------------------
        // 6. Edit PODCAST
        // ---------------------------------------------------------------------

        // GET: Podcaster/EditPodcast/{podcastId}
        // Loads EditPodcast confirmation page
        public async Task<IActionResult> EditPodcast(int podcastId)
        {
            var podcast = await _podcastRepository.GetPodcastByIdAsync(podcastId);

            if (podcast == null)
            {
                return NotFound();
            }

            // Only allow the creator to edit their own podcast
            var currentUserId = Guid.Parse(_userManager.GetUserId(User));
            if (podcast.CreatorID != currentUserId)
            {
                return Forbid();
            }

            return View(podcast); // Passes the podcast to EditPodcast.cshtml
        }


        //// POST: Podcaster/EditPodcastConfirmed/{podcastId}
        [HttpPost, ActionName("EditPodcastConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPodcastConfirmed(int podcastId, string title, string description)
        {
            var userId = Guid.Parse(_userManager.GetUserId(User));
            var existingPodcast = await _podcastRepository.GetPodcastByIdAsync(podcastId);

            if (existingPodcast == null || existingPodcast.CreatorID != userId)
                return NotFound();

            try
            {
                await _podcastRepository.EditPodcastAsync(podcastId, title, description);
                TempData["SuccessMessage"] = $"Podcast '{title}' updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit podcast {PodcastId}.", podcastId);
                TempData["Error"] = "Failed to update podcast due to a database error.";
            }


            return RedirectToAction(nameof(Dashboard));
        }

        // ---------------------------------------------------------------------
        // 7. Analytics
        // ---------------------------------------------------------------------

        [Authorize(Roles = "Podcaster, Admin")]
        public async Task<IActionResult> EpisodeStats(int podcastId)
        {
            // 1. Fetch RDBMS data needed for display (Podcast and Episodes)
            var podcast = await _podcastRepository.GetPodcastByIdAsync(podcastId);

            if (podcast == null)
            {
                TempData["Error"] = "Podcast not found.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Fetch all episodes belonging to this podcast to get their titles AND ReleaseDate
            var allEpisodes = await _episodeRepository.GetEpisodesByPodcastIdAsync(podcastId);

            var episodeMap = allEpisodes.ToDictionary(e => e.EpisodeID, e => e);

            // 2. Get Summary Data from DynamoDB
            var summaries = await _analyticsRepository.GetEpisodeSummariesByPodcastIdAsync(podcastId);

            // 3. Enrich and Sort the Data
            if (summaries != null)
            {
                foreach (var summary in summaries)
                {
                    // Populate the RDBMS data (Title and ReleaseDate) onto the DynamoDB summary object
                    if (episodeMap.ContainsKey(summary.EpisodeID))
                    {
                        var episodeData = episodeMap[summary.EpisodeID];

                        summary.EpisodeTitle = episodeData.Title;

                        summary.ReleaseDate = episodeData.ReleaseDate;
                    }
                    // If the episode is deleted from the RDBMS, we can skip it or mark it
                    else
                    {
                        summary.EpisodeTitle = "[Deleted Episode]";
                        // Initialize date for consistency, assuming a default or min date
                        summary.ReleaseDate = DateTime.MinValue;
                    }
                    summary.PodcastTitle = podcast.Title;
                }

                // Sort by ViewCount descending (default, but now the view can override this)
                summaries = summaries
                                    .OrderByDescending(s => s.ViewCount)
                                    .ToList();
            }
            else
            {
                summaries = new List<EpisodeSummary>();
            }

            // 4. Prepare and return the View Model
            var model = new EpisodeStatsViewModel
            {
                PodcastTitle = podcast.Title,
                TopEpisodes = summaries
            };

            return View(model);
        }

        // Search 

        // GET: Podcaster/SearchEpisodes?podcastId=5&query=interview&searchBy=Topic
        [Authorize(Roles = "Podcaster, Admin")]
        public async Task<IActionResult> SearchEpisodes(int podcastId, string query, string searchBy)
        {
            // 1. Basic validation
            if (string.IsNullOrWhiteSpace(query))
            {
                TempData["Error"] = "Please enter a search term.";
                return RedirectToAction(nameof(ManagePodcast), new { podcastId });
            }

            // 2. Security Check: Ensure the user owns the podcast
            var podcast = await _podcastRepository.GetPodcastByIdAsync(podcastId);
            var currentUserId = Guid.Parse(_userManager.GetUserId(User));

            if (podcast == null || podcast.CreatorID != currentUserId)
            {
                return Forbid();
            }

            // 3. Execute the search
            var results = await _episodeRepository.SearchEpisodesAsync(podcastId, query, searchBy);

            // 4. Prepare the results view model
            var viewModel = new ChannelDetailsViewModel
            {
                Channel = podcast,
                Episodes = results.ToList()
            };

            // 5. Use a dedicated view or reuse the ManagePodcast view
            ViewData["SearchQuery"] = query;
            ViewData["SearchType"] = searchBy;

           
            return View("SearchResults", viewModel);
        }
    }
}