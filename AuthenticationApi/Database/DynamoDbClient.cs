using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime.CredentialManagement;

namespace AuthenticationApi.Database
{
    public class DynamoDbClient : IDatabaseClient
    {
        private readonly AmazonDynamoDBClient _client;

        public DynamoDbClient()
        {
            CredentialProfileStoreChain chain = new CredentialProfileStoreChain();
            chain.TryGetAWSCredentials("AuthenticationApi", out var awsCredentials);

            _client = new AmazonDynamoDBClient(awsCredentials, RegionEndpoint.EUWest2);
        }

        public AmazonDynamoDBClient Client => _client;
    }
}
