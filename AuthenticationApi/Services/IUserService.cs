using AuthenticationApi.Model;

namespace AuthenticationApi.Services
{
    public interface IUserService
    {
        Task<User> Get(string userName);
        Task<bool> Add(string userName, string password);
        Task <bool> Delete(string userName);
        Task UpdatePermissions(string userName, List<string> permissions);
        Task<AuthenticationResult> RefreshToken(string jwtToken, string refreshToken);
        Task<AuthenticationResult> Authenticate(string userName, string password);
    }
}
