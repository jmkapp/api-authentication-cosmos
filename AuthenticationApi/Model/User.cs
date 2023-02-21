using Newtonsoft.Json;

namespace AuthenticationApi.Model
{
    public class User
    {
        [JsonProperty(PropertyName = "id")]
        public string UserName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public List<Permission> Permissions { get; set; } = new List<Permission>();
        public RefreshToken RefreshToken { get; set; }
    }
}
