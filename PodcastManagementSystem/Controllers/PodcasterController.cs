using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PodcastManagementSystem.Interfaces;
using PodcastManagementSystem.Models;
using PodcastManagementSystem.Models.ViewModels;

namespace PodcastManagementSystem.Controllers
{
    // 🛡️ SECURITY: Only users assigned the "Podcaster" role can access this controller.
    [Authorize(Roles = "Podcaster")]
    public class PodcasterController : Controller
    {
        private readonly IPodcastRepository _podcastRepository;
        private readonly IS3Service _s3Service;
        private readonly UserManager<ApplicationUser> _userManager;

        public PodcasterController(
            IPodcastRepository podcastRepository,
            IS3Service s3Service,
            UserManager<ApplicationUser> userManager)
        {
            _podcastRepository = podcastRepository;
            _s3Service = s3Service;
            _userManager = userManager;
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
                // Should not happen if Authorize attribute is working, but safe check.
                return Unauthorized();
            }

            var creatorIdGuid = Guid.Parse(userId);

            // Retrieve podcasts using the new repository method
            var myPodcasts = await _podcastRepository.GetPodcastsByCreatorIdAsync(creatorIdGuid);

            return View(myPodcasts);
        }

        // ---------------------------------------------------------------------
        // 2. CREATE PODCAST (CREATE)
        // ---------------------------------------------------------------------

        // GET: Podcaster/CreatePodcast
        public IActionResult CreatePodcast()
        {
            // Returning an empty ViewModel for the form
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

            // Map ViewModel to the domain model
            var podcast = new Podcast
            {
                Title = model.Title,
                Description = model.Description,
                CreatorID = Guid.Parse(userId),
                CreatedDate = DateTime.UtcNow
            };

            await _podcastRepository.AddPodcastAsync(podcast);

            // Redirect to the new podcast's episode list, or back to the dashboard
            return RedirectToAction(nameof(Dashboard));
        }

        // ---------------------------------------------------------------------
        // 3. EPISODE MANAGEMENT (UPLOAD)
        // ---------------------------------------------------------------------

        // GET: Podcaster/AddEpisode/{podcastId}
        public async Task<IActionResult> AddEpisode(int podcastId)
        {
            var podcast = await _podcastRepository.GetPodcastByIdAsync(podcastId);

            if (podcast == null || podcast.CreatorID != Guid.Parse(_userManager.GetUserId(User)))
            {
                return NotFound(); // Podcast not found or not owned by current user
            }

            // You would pass an EpisodeViewModel to the View here
            var viewModel = new EpisodeViewModel { PodcastID = podcastId };
            return View(viewModel);
        }

        // POST: Podcaster/AddEpisode
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEpisode(EpisodeViewModel model)
        {
            if (!ModelState.IsValid || model.AudioFile == null)
            {
                return View(model);
            }

            // 1. Upload the file to S3
            // The file name should be unique, e.g., using a Guid
            var fileKey = $"episodes/{model.PodcastID}/{Guid.NewGuid()}-{model.AudioFile.FileName}";
            var audioFileUrl = await _s3Service.UploadFileAsync(model.AudioFile, fileKey);

            if (string.IsNullOrEmpty(audioFileUrl))
            {
                ModelState.AddModelError("", "Error uploading file to S3 storage.");
                return View(model);
            }

            // 2. Save metadata to the database
            var episode = new Episode
            {
                PodcastID = model.PodcastID,
                Title = model.Title,
                ReleaseDate = DateTime.UtcNow,
                AudioFileURL = audioFileUrl,
                // You may calculate DurationMinutes here if needed, or rely on user input
                DurationMinutes = model.DurationMinutes
            };

            await _podcastRepository.AddEpisodeAsync(episode);

            // 3. Redirect to the episode listing
            return RedirectToAction(nameof(Dashboard));
        }

        // ---------------------------------------------------------------------
        // 4. CLEANUP (PLACEHOLDER)
        // ---------------------------------------------------------------------

        // Here is where Edit/Delete actions for Podcasts and Episodes here.

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
        // ➜ Actually deletes the podcast
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