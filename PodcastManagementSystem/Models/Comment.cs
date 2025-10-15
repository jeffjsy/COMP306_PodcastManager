using Amazon.DynamoDBv2.DataModel;
using System;

namespace PodcastManagementSystem.Models

{
    [DynamoDBTable("PodcastComments")]
    public class Comment
    {
        // CommentID (Primary Key/Partition Key in DynamoDB) 
        [DynamoDBHashKey] // Hash key (Partition Key)
        public string CommentID { get; set; }

        // EpisodeID 
        
        public int EpisodeID { get; set; }

        // PodcastID
        public int PodcastID { get; set; }

        // UserID (Who wrote the comment)
        public string UserID { get; set; }

        // Text (The comment content) 
        public string Text { get; set; }

        // Timestamp
        public DateTime Timestamp { get; set; }
    }
}
