namespace AuthenticationApi.Model
{
    public class User
    {
        public string UserName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public List<Permission> Permissions { get; set; } = new List<Permission>();
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenCreated { get; set; }
        public DateTime RefreshTokenExpiry { get; set; }
    }
}
