using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using AuthenticationApi.Database;
using AuthenticationApi.Model;

namespace AuthenticationApi.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly string _tableName = "AuthenticationApi";
        private readonly AmazonDynamoDBClient _databaseClient;

        public UserRepository(IDatabaseClient databaseClient)
        {
            _databaseClient = databaseClient.Client;
        }

        public async Task<User> Get(string userName)
        {
            GetItemRequest request = new GetItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>()
                {
                    {"UserName", new AttributeValue() { S = userName }}
                }
            };

            GetItemResponse response = await _databaseClient.GetItemAsync(request);
            Dictionary<string, AttributeValue> fields = response.Item;

            if (fields.Any())
            {
                return new User()
                {
                    UserName = fields["UserName"].S,
                    PasswordHash = fields["Password"].S,
                    Permissions = ParsePermissions(fields["Permissions"].NS),
                    RefreshToken = fields["RefreshToken"].M["Token"].S,
                    RefreshTokenCreated = DateTime.Parse(fields["RefreshToken"].M["Created"].S),
                    RefreshTokenExpiry = DateTime.Parse(fields["RefreshToken"].M["Expiry"].S)
                };
            }

            return new User();
        }

        private List<Permission> ParsePermissions(List<string> permissionValues)
        {
            return permissionValues.Select(permissionValue => (Permission)Convert.ToInt32(permissionValue)).ToList();
        }

        public async Task<bool> Add(User newUser)
        {
            Dictionary<string, AttributeValue> refreshToken = new Dictionary<string, AttributeValue>()
            {
                { "Token", new AttributeValue { S = newUser.RefreshToken == null ? string.Empty : newUser.RefreshToken } },
                { "Created", new AttributeValue { S = newUser.RefreshTokenCreated == null ? DateTime.MinValue.ToString(CultureInfo.CurrentCulture) : newUser.RefreshTokenCreated.ToString(CultureInfo.CurrentCulture) } },
                { "Expiry", new AttributeValue { S = newUser.RefreshTokenExpiry == null ? DateTime.MinValue.ToString(CultureInfo.CurrentCulture) : newUser.RefreshTokenExpiry.ToString(CultureInfo.CurrentCulture) } }
            };

            PutItemRequest request = new PutItemRequest
            {
                TableName = _tableName,
                Item = new Dictionary<string, AttributeValue>
                {
                    { "UserName", new AttributeValue { S = newUser.UserName } },
                    { "Password", new AttributeValue { S = newUser.PasswordHash } },
                    { "Permissions", new AttributeValue { NS = newUser.Permissions.Select(p => (int)p).Select(p => p.ToString()).ToList() } },
                    { "RefreshToken", new AttributeValue { M = refreshToken } }
                },
                ConditionExpression = "attribute_not_exists(UserName)"
            };

            try
            {
                await _databaseClient.PutItemAsync(request);
            }
            catch (ConditionalCheckFailedException)
            {
                return false;
            }

            return true;
        }

        public async Task<bool> Delete(string userName)
        {
            DeleteItemRequest request = new DeleteItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "UserName", new AttributeValue { S = userName} }
                },
                ConditionExpression = "attribute_exists(UserName)"
            };

            try
            {
                await _databaseClient.DeleteItemAsync(request);
            }
            catch (ConditionalCheckFailedException)
            {
                return false;
            }

            return true;
        }

        public async Task UpdatePermissions(string userName, List<Permission> permissions)
        {
            List<string> permissionValues = permissions.Select(p => (int)p).Select(p => p.ToString()).ToList();

            UpdateItemRequest request = new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "UserName", new AttributeValue { S = userName } }
                },
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#P", "Permissions" }
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    { ":perm", new AttributeValue { NS = permissionValues } }
                },
                UpdateExpression = "SET #P = :perm"
            };

           UpdateItemResponse? response =  await _databaseClient.UpdateItemAsync(request);
        }

        public async Task SetRefreshToken(string userName, RefreshToken refreshToken)
        {
            Dictionary<string, AttributeValue> newRefreshToken = new Dictionary<string, AttributeValue>()
            {
                { "Token", new AttributeValue { S = refreshToken.Token == null ? string.Empty : refreshToken.Token } },
                { "Created", new AttributeValue { S = refreshToken.Created == null ? DateTime.MinValue.ToString(CultureInfo.CurrentCulture) : refreshToken.Created.ToString(CultureInfo.CurrentCulture) } },
                { "Expiry", new AttributeValue { S = refreshToken.Expiry == null ? DateTime.MinValue.ToString(CultureInfo.CurrentCulture) : refreshToken.Expiry.ToString(CultureInfo.CurrentCulture) } }
            };

            UpdateItemRequest request = new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "UserName", new AttributeValue { S = userName } }
                },
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#T", "RefreshToken" }
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":token", new AttributeValue { M = newRefreshToken } }
                },
                UpdateExpression = "SET #T = :token"
            };

            UpdateItemResponse? response = await _databaseClient.UpdateItemAsync(request);
        }
    }
}
