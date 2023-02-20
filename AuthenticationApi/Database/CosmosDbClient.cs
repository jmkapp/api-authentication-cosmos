using Microsoft.Azure.Cosmos;

namespace AuthenticationApi.Database
{
    public class CosmosDbClient : ICosmosDbClient
    {
        private readonly CosmosClient _client;

        public CosmosDbClient(IConfiguration configuration)
        {
            _client = new CosmosClient(configuration["CosmosDb:Endpoint"], configuration["CosmosDb:AuthorisationKey"]);
        }

        public CosmosClient Client => _client;
    }
}
