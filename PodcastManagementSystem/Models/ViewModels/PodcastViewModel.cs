using System.ComponentModel.DataAnnotations;

namespace PodcastManagementSystem.Models.ViewModels
{
    // This class is used to transfer form data from the View to the Controller
    public class PodcastViewModel
    {
        [Required(ErrorMessage = "A Title is required.")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        [Display(Name = "Podcast Title")]
        public string Title { get; set; }

        [Required(ErrorMessage = "A Description is required.")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Channel Description")]
        public string Description { get; set; }
    }
}

