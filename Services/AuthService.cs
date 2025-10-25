using Waster.Helpers;
using Waster.Models;
using Waster.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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
            if (await _userManager.FindByEmailAsync(model.Email) is not null)
            { return new AuthModel { Message = "Email is already registered!" }; }
            if (await _userManager.FindByNameAsync(model.Email) is not null)
            { return new AuthModel { Message = "Username is already registered!" }; }

            var user = new AppUser
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                UserName = model.Email,
                SecurityStamp = Guid.NewGuid().ToString()
            };


            var result = await _userManager.CreateAsync(user, model.Password);
            return result.Succeeded ? new AuthModel { Message = "User created successfully!" } : new AuthModel
            {
                Message = "User did not create successfully! Please try again."
                    + string.Join(", ", result.Errors.Select(e => e.Description))
            };

            var jwtSecurityToken = await CreateJwtToken(user);
            return new AuthModel
            {
                Email = user.Email,
                IsAuthenticated = true,
                Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken),
               // Expiration = jwtSecurityToken.ValidTo,
                UserName = user.UserName,
                Roles = (await _userManager.GetRolesAsync(user)).ToList()
            };

        }

        public async Task<AuthModel> LoginAsync(LoginModel model)
        {
            var authmodel = new AuthModel();
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user is null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                authmodel.Message = "Email or Password is Incorrect!";
            }
            var jwtSecurityToken = await CreateJwtToken(user);
            var roles = await _userManager.GetRolesAsync(user);
            var userClaims = await _userManager.GetClaimsAsync(user);
            authmodel.Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);

            authmodel.IsAuthenticated = true;
            authmodel.Email = user.Email;
           // authmodel.Expiration = jwtSecurityToken.ValidTo;
            authmodel.UserName = user.UserName;
            authmodel.Roles = roles.ToList();
            authmodel.Message = "Login Successful!";
            if (user.RefreshTokens.Any(t => t.IsActive))
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
                user.RefreshTokens.Add(refreshToken);
                await _userManager.UpdateAsync(user);

            }
            return authmodel;

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
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("uid", user.Id)
            }
            .Union(userClaims)
            .Union(roleClaims);

            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.Now.AddDays(_jwt.DurationInDays),
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
                Token = Convert.ToBase64String(randomNumber),
                ExpiresOn = DateTime.UtcNow.AddDays(7),
                CreatedOn = DateTime.UtcNow

            };

        }


        public async Task<AuthModel> RefreshTokenAsync(string token)
        {
            var authmodel = new AuthModel();
            var user = await _userManager.Users.SingleOrDefaultAsync(t=> t.RefreshTokens.Any(r=> r.Token==token));
            if (user==null)
            {
                authmodel.IsAuthenticated = false;
                authmodel.Message= "Invalid Token";
                return authmodel;

            }
            var refreshtoken = user.RefreshTokens.Single(u => u.Token == token);
            if (!refreshtoken.IsActive) {
                authmodel.IsAuthenticated = false;
                authmodel.Message = "Invalid Token";
                return authmodel;
            }
            refreshtoken.RevokeOn= DateTime.UtcNow;
            var NewRefreshToken= GenerateRefreshToken();
            user.RefreshTokens.Add(NewRefreshToken);

            await _userManager.UpdateAsync(user);
            var JwtToken =await CreateJwtToken(user);
            authmodel.IsAuthenticated = true;
            authmodel.Email=user.Email;
            authmodel.UserName=user.UserName;
            authmodel.Token = new JwtSecurityTokenHandler().WriteToken(JwtToken);
            var roles = await _userManager.GetRolesAsync(user);
            authmodel.Roles=roles.ToList();
            authmodel.RefreshToken=NewRefreshToken.Token;
            authmodel.RefreshTokenExpiration = NewRefreshToken.ExpiresOn;
            return authmodel;


        }
        public async Task<bool> RevokeTokenAsync(string token)
        {


            var user = await _userManager.Users.SingleOrDefaultAsync(t => t.RefreshTokens.Any(r => r.Token == token));
            if (user == null)
                return false;


            var refreshtoken = user.RefreshTokens.Single(u => u.Token == token);
            if (!refreshtoken.IsActive)
                return false;

            refreshtoken.RevokeOn = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);
            return true;



        }
    }
}
