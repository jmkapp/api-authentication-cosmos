using Microsoft.Azure.Cosmos;

namespace AuthenticationApi.Database
{
    public interface ICosmosDbClient
    {
        CosmosClient Client { get; }
    }
}
