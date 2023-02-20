using AuthenticationApi.Database;
using AuthenticationApi.Model;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Permission = AuthenticationApi.Model.Permission;
using User = AuthenticationApi.Model.User;

namespace AuthenticationApi.Repositories
{
    public class CosmosDbRepository : IUserRepository
    {
        private readonly CosmosClient _databaseClient;
        private readonly Container _container;

        public CosmosDbRepository(IConfiguration configuration, ICosmosDbClient client)
        {
            _databaseClient = client.Client;
            _container = _databaseClient.GetDatabase(configuration["CosmosDb:AuthenticationApi:DatabaseName"]).GetContainer(configuration["CosmosDb:AuthenticationApi:UserContainer"]);
        }

        public async Task<User> Get(string userName)
        {
            User user = null;

            using (FeedIterator<User> iterator = _container.GetItemLinqQueryable<User>().Where(u => u.UserName == userName).ToFeedIterator())
            {
                while (iterator.HasMoreResults && user == null)
                {
                    FeedResponse<User> response = await iterator.ReadNextAsync();
                    user = response.Resource.FirstOrDefault();
                }
            }

            return user;
        }

        public async Task<bool> Add(User newUser)
        {
            User existingUser = await Get(newUser.UserName);

            if (existingUser != null)
            {
                return false;
            }

            await _container.CreateItemAsync(newUser);

            return true;
        }

        public async Task<bool> Delete(string userName)
        {
            User user = await Get(userName);

            if (user != null)
            {
                await _container.DeleteItemAsync<User>(user.UserName, new PartitionKey(user.UserName));
                return true;
            }

            return false;
        }

        public async Task UpdatePermissions(string userName, List<Permission> permissions)
        {
            User user = await Get(userName);

            user.Permissions = permissions;

            await _container.UpsertItemAsync(user);
        }

        public async Task SetRefreshToken(string userName, RefreshToken refreshToken)
        {
            User user = await Get(userName);
            user.RefreshToken = refreshToken;

            await _container.UpsertItemAsync(user);
        }
    }
}
