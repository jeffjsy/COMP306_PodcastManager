using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace PodcastManagementSystem.Models.ViewModels
{
    public class EpisodeViewModel
    {
        // Links the episode to its parent podcast
        [Required]
        public int PodcastID { get; set; }

        [Required(ErrorMessage = "Episode title is required.")]
        [StringLength(150)]
        public string Title { get; set; }

        // Field for the audio file upload
        [Required(ErrorMessage = "An audio file is required.")]
        public IFormFile AudioFile { get; set; }

        [Required(ErrorMessage = "Duration in minutes is required.")]
        [Range(1, 1440, ErrorMessage = "Duration must be a positive number.")]
        public int DurationMinutes { get; set; }
    }
}

