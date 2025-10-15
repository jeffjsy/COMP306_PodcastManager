using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PodcastManagementSystem.Models
{
    public class Subscription
    {
        // SubscriptionID (PK) 
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SubscriptionID { get; set; }

        // UserID (Foreign Key to the Users table) 
        [Required]
        public string UserID { get; set; }

        // PodcastID (Foreign Key to the Podcast table) 
        public int PodcastID { get; set; }

        // SubscribedDate 
        public DateTime SubscribedDate { get; set; }

        // Navigation property for the parent Podcast
        [ForeignKey("PodcastID")]
        public Podcast Podcast { get; set; }
    }
}
