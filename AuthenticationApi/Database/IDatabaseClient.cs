using Amazon.DynamoDBv2;

namespace AuthenticationApi.Database
{
    public interface IDatabaseClient
    {
        AmazonDynamoDBClient Client { get; }
    }
}
