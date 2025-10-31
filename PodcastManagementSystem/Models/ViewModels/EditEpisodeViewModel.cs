using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using PodcastManagementSystem.Models;

namespace PodcastManagementSystem.Models.ViewModels
{
    public class EditEpisodeViewModel
    {
        public int Id { get; set; } 
        public int PodcastID { get; set; }

        [Required(ErrorMessage = "Episode Title is required.")]
        [StringLength(100)]
        public string Title { get; set; }

        [Required(ErrorMessage = "A brief description is required.")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        // DurationMinutes is still system-set, but we'll display the current value
        [Display(Name = "Current Duration (Minutes)")]
        public int DurationMinutes { get; set; }

        // ⚠️ NOT [Required]: This is optional if only metadata is being updated
        [Display(Name = "Update Audio File (Optional)")]
        public IFormFile AudioFile { get; set; }

        // 🌟 Current file URL, helpful for display purposes
        public string CurrentAudioFileURL { get; set; }
    }
}