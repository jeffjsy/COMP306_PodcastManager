using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PodcastManagementSystem.Models
{
    public class Episode
    {
        // EpisodeID (PK, int, auto-increment) 
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EpisodeID { get; set; }

        // PodcastID (Foreign Key) 
        [ForeignKey("PodcastID")]
        public int PodcastID { get; set; }


        // Episode Title 
        [Required]
        public string? Title { get; set; }

        // Episode Description
        [Required]
        public string? Description { get; set; }

        // Release Date
        [DataType(DataType.Date)]
        public DateTime ReleaseDate { get; set; }

        // Duration (in minutes) 
        public int DurationMinutes { get; set; }

        // PlayCount (i.e., number of viewers) 
        public int PlayCount { get; set; }

        // AudioFileURL // Link to S3 object 
        public string AudioFileURL { get; set; }

        // Navigation properties
        [ForeignKey("PodcastID")]
        public Podcast Podcast { get; set; }
    }
}
