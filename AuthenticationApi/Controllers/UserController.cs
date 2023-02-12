using AuthenticationApi.Model;
using AuthenticationApi.Services;
using AuthenticationApi.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AuthenticationApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IUserService _userService;

        public UserController(IConfiguration configuration, IUserService userService)
        {
            _configuration = configuration;
            _userService = userService;
        }

        [HttpGet("{userName}")]
        [Authorize(Roles = "GetUser")]
        public async Task<UserViewModel> Get(string userName)
        {
            User user =  await _userService.Get(userName);

            UserViewModel userViewModel = new UserViewModel()
            {
                UserName = user.UserName != null ? user.UserName : string.Empty,
                Password = string.Empty,
                Permissions = new Permissions().GetPermissionNames(user.Permissions)
            };

            return userViewModel;
        }

        [HttpPost("Add")]
        [Authorize(Roles = "AddUser")]
        public async Task<bool> Add(UserViewModel user)
        {
            try
            {
                return await _userService.Add(user.UserName, user.Password);
            }
            catch (Exception e)
            {
                return false;
            }
        }

        [HttpDelete("{userName}")]
        [Authorize(Roles = "DeleteUser")]
        public async Task<bool> Delete(string userName)
        {
            try
            {
                return await _userService.Delete(userName);
            }
            catch (Exception)
            {
                return false;
            }
        }

        [HttpPost("Permission")]
        [Authorize(Roles = "UpdatePermission")]
        public async Task<ActionResult> Permission(PermissionViewModel permissions)
        {
            if (string.IsNullOrWhiteSpace(permissions.UserName))
            {
                return BadRequest("Invalid username.");
            }

            await _userService.UpdatePermissions(permissions.UserName, permissions.Permissions);

            return Ok();
        }

        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<ActionResult<string>> Login(UserViewModel userModel)
        {
            AuthenticationResult result = await _userService.Authenticate(userModel.UserName, userModel.Password);

            if (!result.UserAuthenticated)
            {
                return BadRequest("Invalid credentials.");
            }

            CookieOptions cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = result.RefreshToken.Expiry
            };

            Response.Cookies.Append("RefreshToken", result.RefreshToken.Token, cookieOptions);

            return Ok(result.JwtToken);
        }



        [HttpGet("RefreshToken")]
        [AllowAnonymous]
        public async Task<ActionResult<string>> RefreshToken()
        {
            string? refreshToken = Request.Cookies["RefreshToken"];

            string jwtToken = Request.Headers.Authorization;

            AuthenticationResult result = await _userService.RefreshToken(jwtToken, refreshToken);

            if (!result.UserAuthenticated)
            {
                return BadRequest("Invalid credentials.");
            }

            CookieOptions cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = result.RefreshToken.Expiry
            };

            Response.Cookies.Append("RefreshToken", result.RefreshToken.Token, cookieOptions);

            return Ok(result.JwtToken);
        }
    }
}
