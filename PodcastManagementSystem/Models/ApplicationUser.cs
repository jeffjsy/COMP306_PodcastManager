using Microsoft.AspNetCore.Identity;

namespace PodcastManagementSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Role { get; set; } // Podcaster, Listener/viewer, Admin
    }
}
