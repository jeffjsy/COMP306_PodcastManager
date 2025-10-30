using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PodcastManagementSystem.Interfaces;
using PodcastManagementSystem.Models;
using PodcastManagementSystem.Models.ViewModels;
namespace PodcastManagementSystem.Controllers
{

    public class ListenerViewerController : Controller
    {
        private readonly IPodcastRepository _podcastRepository;
        // NOTE: For best practice, you should inject IEpisodeRepository here if episodes 
        // are managed separately from the podcast object in your repository layer.

        public ListenerViewerController(IPodcastRepository podcastRepository)
        {
            _podcastRepository = podcastRepository;
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

            return View(viewModel); // <-- Updated to return the ViewModel
        }
    }
}