namespace PodcastManagementSystem.Models.ViewModels
{
    // This model combines the Podcast (channel details) and its Episodes for the Listener view.
    public class ChannelDetailsViewModel
    {
        // Full details of the main podcast channel
        public Podcast Channel { get; set; }

        // List of all episodes belonging to this channel
        public List<Episode> Episodes { get; set; }

        // Optional: Could include the creator's name/profile here if needed
        // public string CreatorName { get; set; }
    }
}
