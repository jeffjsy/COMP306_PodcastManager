using Amazon.DynamoDBv2.DataModel;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PodcastManagementSystem.Models

{
    [DynamoDBTable("PodcastComments")]
    public class Comment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CommentID { get ; set; }

        // Foreign Key to Episode
        [Required]
        [ForeignKey("EpisodeID")]
        public int EpisodeID { get; set; }

        // Foreign Key to ApplicationUser (The Comment Author)
        [Required]
        public Guid UserID { get; set; } // Stored as string to match Identity User ID type

        // Comment Content
        [Required(ErrorMessage = "Comment content is required.")]
        [StringLength(500, ErrorMessage = "Comment cannot exceed 500 characters.")]
        public string Text { get; set; }

        // Timestamp for Creation
        public DateTime TimeStamp { get; set; }

        // Navigation Properties
        public Episode Episode { get; set; }

        // Navigation property to IdentityUser
        // We link to ApplicationUser, which inherits from IdentityUser
        [ForeignKey("UserID")]
        public ApplicationUser User { get; set; }
    }
}
