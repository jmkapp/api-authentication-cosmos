using AuthenticationApi.Model;

namespace AuthenticationApi.Repositories
{
    public interface IUserRepository
    {
        Task<User> Get(string userName);
        Task<bool> Add(User newUser);
        Task<bool> Delete(string userName);
        Task UpdatePermissions(string userName, List<Permission> permissions);
        Task SetRefreshToken(string userName, RefreshToken refreshToken);
    }
}
