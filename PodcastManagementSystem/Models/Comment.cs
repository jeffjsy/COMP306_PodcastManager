using Amazon.DynamoDBv2.DataModel;
using PodcastManagementSystem.Models;

// Name of table on DynamoDB
[DynamoDBTable("Comments")] // 
public class Comment
{
    // Partition Key 
    [DynamoDBHashKey]
    public int EpisodeID { get; set; }

    // Sort Key
    [DynamoDBRangeKey]
    public Guid CommentID { get; set; }


    public int PodcastID { get; set; } 

    public Guid UserID { get; set; }

    [DynamoDBProperty("CommentText")] // Use a descriptive name in DynamoDB
    public string Text { get; set; }

    [DynamoDBProperty]
    public DateTime TimeStamp { get; set; }

    [DynamoDBIgnore]
    public ApplicationUser User { get; set; }
}