using AuthenticationApi.Repositories;
using AuthenticationApi.Model;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Cryptography;

namespace AuthenticationApi.Services
{
    public class UserService : IUserService
    {
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly string _jwtKey;

        public UserService(IConfiguration configuration, IUserRepository userRepository, TokenValidationParameters tokenValidationParameters)
        {
            _configuration = configuration;
            _userRepository = userRepository;
            _tokenValidationParameters = tokenValidationParameters.Clone();
            _tokenValidationParameters.ValidateLifetime = false;

            _jwtKey = _configuration["Jwt:Key"];
        }

        public async Task<User> Get(string userName)
        {
            return await _userRepository.Get(userName);
        }

        public async Task<bool> Add(string userName, string password)
        {
            User newUser = new User()
            {
                UserName = userName
            };

            newUser.PasswordHash = new PasswordHasher<User>().HashPassword(newUser, password);
            newUser.Permissions = new List<Permission> { Permission.GetUser };

            return await _userRepository.Add(newUser);
        }

        public async Task<bool> Delete(string userName)
        {
            return await _userRepository.Delete(userName);
        }

        private static bool VerifyPassword(User user, string password)
        {
            PasswordVerificationResult verifiedResult = new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, password);

            return verifiedResult == PasswordVerificationResult.Success;
        }

        public async Task UpdatePermissions(string userName, List<string> permissions)
        {
            await _userRepository.UpdatePermissions(userName, new Permissions().GetPermissions(permissions));
        }

        public async Task<AuthenticationResult> Authenticate(string userName, string password)
        {
            AuthenticationResult result = new AuthenticationResult();

            User user = await Get(userName);

            if (user == null)
            {
                return result;
            }

            bool verified = VerifyPassword(user, password);

            if (!verified)
            {
                return result;
            }

            result.UserAuthenticated = true;
            result.JwtToken = CreateJwtToken(user);
            result.RefreshToken = GenerateRefreshToken();

            await _userRepository.SetRefreshToken(userName, result.RefreshToken);

            return result;
        }

        public async Task<AuthenticationResult> RefreshToken(string jwtToken, string refreshToken)
        {
            AuthenticationResult result = new AuthenticationResult();

            ClaimsPrincipal principal = GetPrincipalFromToken(jwtToken);
            
            if (principal == null)
            {
                return result;
            }

            string? userName = principal.Identities.First().Name;

            if (string.IsNullOrWhiteSpace(userName))
            {
                return result;
            }

            User user = await Get(userName);

            if (user == null || user.RefreshToken != refreshToken || DateTime.Now > user.RefreshTokenExpiry)
            {
                return result;
            }

            result.UserAuthenticated = true;
            result.JwtToken = CreateJwtToken(user);
            result.RefreshToken = GenerateRefreshToken();

            await _userRepository.SetRefreshToken(userName, result.RefreshToken);

            return result;
        }

        private ClaimsPrincipal GetPrincipalFromToken(string token)
        {
            token = token.Replace("bearer ", string.Empty);

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                ClaimsPrincipal? principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);

                if (!IsJwtWithValidSecurityAlgorithm(validatedToken))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        private bool IsJwtWithValidSecurityAlgorithm(SecurityToken validatedToken)
        {
            return (validatedToken is JwtSecurityToken jwtSecurityToken) &&
                   jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha512Signature,
                       StringComparison.InvariantCultureIgnoreCase);
        }

        private string CreateJwtToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName)
            };

            claims.AddRange(new Permissions().GetPermissionNames(user.Permissions).Select(permission => new Claim(ClaimTypes.Role, permission)));

            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));

            SigningCredentials creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            TimeSpan jwtExpiry = new TimeSpan(Int32.Parse(_configuration["JwtTokenExpiry:Days"]), Int32.Parse(_configuration["JwtTokenExpiry:Hours"]),
                Int32.Parse(_configuration["JwtTokenExpiry:Minutes"]), Int32.Parse(_configuration["JwtTokenExpiry:Seconds"]));

            JwtSecurityToken token = new JwtSecurityToken(
                claims: claims,
                signingCredentials: creds,
                expires: DateTime.Now.Add(jwtExpiry));

            string jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }

        private RefreshToken GenerateRefreshToken()
        {
            TimeSpan refreshTokenExpiry = new TimeSpan(Int32.Parse(_configuration["RefreshTokenExpiry:Days"]), Int32.Parse(_configuration["RefreshTokenExpiry:Hours"]),
                Int32.Parse(_configuration["RefreshTokenExpiry:Minutes"]), Int32.Parse(_configuration["RefreshTokenExpiry:Seconds"]));

            RefreshToken refreshToken = new RefreshToken()
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                Expiry = DateTime.Now.Add(refreshTokenExpiry),
                Created = DateTime.Now
            };

            return refreshToken;
        }
    }
}
