using Amazon.DynamoDBv2.DataModel;

[DynamoDBTable("EpisodeSummary")]
public class EpisodeSummary
{
    [DynamoDBHashKey]
    public int PodcastID { get; set; }

    [DynamoDBRangeKey]
    public int EpisodeID { get; set; }

    // Aggregated metrics
    public long ViewCount { get; set; }
    public long CommentCount { get; set; }


    [DynamoDBIgnore]
    public string EpisodeTitle { get; set; }
    [DynamoDBIgnore]
    public string PodcastTitle { get; set; }
}
