using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PodcastManagementSystem.Models
{
    public enum UserRole
    {
        Podcaster = 0,
        ListenerViewer = 1,
        Admin = 2
    }

    public class ApplicationUser : IdentityUser<Guid>
    {
        public UserRole Role { get; set; }
        public List<Subscription> Subscriptions { get; set; } = new(); // Navigation

        [NotMapped]
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
