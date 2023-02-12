using System.ComponentModel.DataAnnotations;

namespace AuthenticationApi.ViewModel
{
    public class UserViewModel
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public List<string>? Permissions { get; set; }
    }
}
