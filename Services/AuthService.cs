using Azure;
using Azure.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Waster.Helpers;
using Waster.Models;
using Waster.Services;

namespace Waster.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly Jwt _jwt;

        public AuthService(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, IOptions<Jwt> jwt)
        {
            _userManager = userManager;
            _jwt = jwt.Value;
            _roleManager = roleManager;
        }

        public async Task<AuthModel> RegisterAsync(RegisterModel model)
        {
            // Check if email already exists
            if (await _userManager.FindByEmailAsync(model.Email) is not null)
            {
                return new AuthModel { Message = "Email is already registered!" };
            }

            // Check if username already exists
            if (await _userManager.FindByNameAsync(model.Email) is not null)
            {
                return new AuthModel { Message = "Username is already registered!" };
            }

            // Create new user
            var user = new AppUser
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                UserName = model.Email,
                Address = model.Address,
                //State = model.State,
                //City = model.City,
                PhoneNumber = model.PhoneNumber,
                SecurityStamp = Guid.NewGuid().ToString(),
                RefreshTokens = new List<RefreshTokens>() // Initialize the collection
            };

            //create user
            var result = await _userManager.CreateAsync(user, model.Password);

            // If creation failed, return error
            if (!result.Succeeded)
            {
                return new AuthModel
                {
                    Message = "User did not create successfully! " +
                              string.Join(", ", result.Errors.Select(e => e.Description))
                };
            }

            // If creation succeeded, generate JWT token and return success
            var jwtSecurityToken = await CreateJwtToken(user);

            var refreshToken = GenerateRefreshToken();
            user.RefreshTokens.Add(refreshToken);
            await _userManager.UpdateAsync(user);

            return new AuthModel
            {
                Email = user.Email,
                IsAuthenticated = true,
                Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken),
                ExpiresOn = jwtSecurityToken.ValidTo,
                UserName = user.UserName,
                Roles = (await _userManager.GetRolesAsync(user)).ToList(),
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiration = refreshToken.ExpiresOn,
                Message = "User registered successfully!"
            };
        }

        // Login Method
        public async Task<AuthModel> LoginAsync(LoginModel model)
        {
            var authmodel = new AuthModel();

            // FIXED: Eagerly load RefreshTokens navigation property
            var user = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user is null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                authmodel.Message = "Email or Password is Incorrect!";
                return authmodel;
            }

            var jwtSecurityToken = await CreateJwtToken(user);
            var roles = await _userManager.GetRolesAsync(user);

            authmodel.Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
            authmodel.IsAuthenticated = true;
            authmodel.Email = user.Email;
            authmodel.ExpiresOn = jwtSecurityToken.ValidTo;
            authmodel.UserName = user.UserName;
            authmodel.Roles = roles.ToList();
            authmodel.Message = "Login Successful!";

            // FIXED: Check if RefreshTokens collection is not null before querying
            if (user.RefreshTokens != null && user.RefreshTokens.Any(t => t.IsActive))
            {
                var activeRefreshToken = user.RefreshTokens.FirstOrDefault(t => t.IsActive);
                authmodel.RefreshToken = activeRefreshToken.Token;
                authmodel.RefreshTokenExpiration = activeRefreshToken.ExpiresOn;
            }
            else
            {
                var refreshToken = GenerateRefreshToken();
                authmodel.RefreshToken = refreshToken.Token;
                authmodel.RefreshTokenExpiration = refreshToken.ExpiresOn;

                // Initialize collection if null
                if (user.RefreshTokens == null)
                    user.RefreshTokens = new List<RefreshTokens>();

                user.RefreshTokens.Add(refreshToken);
                await _userManager.UpdateAsync(user);
            }

            return authmodel;
        }

        public async Task<string> AddRoleAsync(AddRole model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user is null || !await _roleManager.RoleExistsAsync(model.RoleName))
                return "User or Role does not exist!";
            if (await _userManager.IsInRoleAsync(user, model.RoleName))
                return "User already assigned to this role!";

            var result = await _userManager.AddToRoleAsync(user, model.RoleName);

            return result.Succeeded ? "User added to role successfully!"
                : "Error: User could not be added to role.";
        }

        private async Task<JwtSecurityToken> CreateJwtToken(AppUser user)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            var roleClaims = new List<Claim>();

            foreach (var role in roles)
                roleClaims.Add(new Claim("roles", role));

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("username", user.UserName)
            }
            .Union(userClaims)
            .Union(roleClaims);

            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(_jwt.DurationInDays),
                signingCredentials: signingCredentials);

            return jwtSecurityToken;
        }

        private RefreshTokens GenerateRefreshToken()
        {
            var randomNumber = new byte[32];

            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);

            return new RefreshTokens
            {
                Token = Base64UrlEncode(randomNumber),
                ExpiresOn = DateTime.UtcNow.AddDays(7),
                CreatedOn = DateTime.UtcNow
            };
        }
        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        public async Task<AuthModel> RefreshTokenAsync(string token)
        {
            var authmodel = new AuthModel();

            // FIXED: Eagerly load RefreshTokens navigation property
            var user = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .SingleOrDefaultAsync(u => u.RefreshTokens.Any(r => r.Token == token));

            if (user == null)
            {
                authmodel.IsAuthenticated = false;
                authmodel.Message = "Invalid Token";
                return authmodel;
            }

            var refreshtoken = user.RefreshTokens.Single(u => u.Token == token);
            if (!refreshtoken.IsActive)
            {
                authmodel.IsAuthenticated = false;
                authmodel.Message = "Invalid Token";
                return authmodel;
            }

            refreshtoken.RevokeOn = DateTime.UtcNow;
            var NewRefreshToken = GenerateRefreshToken();
            user.RefreshTokens.Add(NewRefreshToken);

            await _userManager.UpdateAsync(user);
            var JwtToken = await CreateJwtToken(user);

            authmodel.IsAuthenticated = true;
            authmodel.Email = user.Email;
            authmodel.UserName = user.UserName;
            authmodel.Token = new JwtSecurityTokenHandler().WriteToken(JwtToken);
            authmodel.ExpiresOn = JwtToken.ValidTo;

            var roles = await _userManager.GetRolesAsync(user);
            authmodel.Roles = roles.ToList();
            authmodel.RefreshToken = NewRefreshToken.Token;
            authmodel.RefreshTokenExpiration = NewRefreshToken.ExpiresOn;

            return authmodel;
        }







        // Add this to your AuthService.cs - Replace the RevokeTokenAsync method

        public async Task<bool> RevokeTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;
            token = token.Trim();

            // FIXED: Eagerly load RefreshTokens navigation property and search properly
            var user = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.RefreshTokens != null &&
                                         u.RefreshTokens.Any(r => r.Token == token));

            if (user == null)
            {
                // Log for debugging
                Console.WriteLine($"No user found with refresh token: {token.Substring(0, Math.Min(10, token.Length))}...");
                return false;
            }

            // Find the specific refresh token
            var refreshtoken = user.RefreshTokens.FirstOrDefault(u => u.Token == token);

            if (refreshtoken == null)
            {
                Console.WriteLine("Refresh token not found in user's collection");
                return false;
            }

            if (!refreshtoken.IsActive)
            {
                Console.WriteLine("Token is already revoked or expired (refreshtoken is not active) ");
                return false;
            }

            // Revoke the token
            refreshtoken.RevokeOn = DateTime.UtcNow;

            try
            {
                await _userManager.UpdateAsync(user);
                Console.WriteLine("Token revoked successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user: {ex.Message}");
                return false;
            }
        }

    }
}