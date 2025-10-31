using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using PodcastManagementSystem.Models; 

namespace PodcastManagementSystem.Models.ViewModels
{
    public class AddEpisodeViewModel
    {
        // Foreign Key to associate the episode with the correct podcast channel
        public int PodcastID { get; set; }

        // Metadata fields for the new episode

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required(ErrorMessage = "A Description is required for the episode.")]
        [StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters.")]
        public string Description { get; set; }

        [Display(Name = "Duration (Minutes)")]
        public int DurationMinutes { get; set; }

        // Field for the actual file upload
        [Required(ErrorMessage = "Please select an audio file to upload.")]
        [Display(Name = "Audio File (MP3, WAV)")]
        public IFormFile AudioFile { get; set; }
    }
}