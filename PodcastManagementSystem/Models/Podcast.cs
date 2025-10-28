using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PodcastManagementSystem.Models
{
    public class Podcast
    {
        // PodcastID (PK, int, auto-increment)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PodcastID { get; set; }

        // Title
        [Required]
        public string Title { get; set; }

        // Description 
        public string Description { get; set; }

        // CreatorID 
        public Guid CreatorID { get; set; }

        // CreatedDate 
        public DateTime CreatedDate { get; set; }

        // Navigation property for related episodes
        public ICollection<Episode> Episodes { get; set; } = new List<Episode>();
        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

    }
}
