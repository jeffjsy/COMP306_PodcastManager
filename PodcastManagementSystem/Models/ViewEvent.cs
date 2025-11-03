// File: Models/ViewEvent.cs

using Amazon.DynamoDBv2.DataModel;

[DynamoDBTable("EpisodeViews")]
public class ViewEvent
{
    [DynamoDBHashKey]
    public int EpisodeID { get; set; }

    [DynamoDBRangeKey]
    public string TimeStamp { get; set; }

    // Additional data (optional, but helpful for deep analytics)
    public Guid? UserID { get; set; } 
    public string IPAddress { get; set; }
}