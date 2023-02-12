using AuthenticationApi.Model;

namespace AuthenticationApi.Services
{
    public class AuthenticationResult
    {
        public bool UserAuthenticated { get; set; }
        public string JwtToken { get; set; }
        public RefreshToken RefreshToken { get; set; }
    }
}
