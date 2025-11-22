using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SqlServer.Server;
using System.Security.Claims;
using Waster.Models;
using Waster.Services;

namespace Waster.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GoogleAuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IAuthService _authService;
        private readonly ILogger<GoogleAuthController> _logger;
        private readonly IConfiguration _configuration;

        public GoogleAuthController(
            UserManager<AppUser> userManager,
            IAuthService authService,
            ILogger<GoogleAuthController> logger,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _authService = authService;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Authenticate with Google ID Token from Flutter
        /// </summary>
        /// <param name="request">Google ID Token from Flutter Google Sign-In</param>
        /// <returns>JWT token and user info</returns>
        [HttpPost("google-signin")]
        public async Task<IActionResult> GoogleSignIn([FromBody] GoogleSignInRequest request)
        {
            if (string.IsNullOrEmpty(request.IdToken))
            {
                return BadRequest(new { message = "ID token is required" });
            }

            try
            {
                // Verify the Google ID token
                var payload = await VerifyGoogleToken(request.IdToken);

                if (payload == null)
                {
                    return BadRequest(new { message = "Invalid Google token" });
                }

                // Extract user info from Google token
                var email = payload.Email;
                var firstName = payload.GivenName;
                var lastName = payload.FamilyName;
                var googleId = payload.Subject;

                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest(new { message = "Email not provided by Google" });
                }

                // Check if user exists
                var user = await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    // Create new user
                    user = new AppUser
                    {
                        Email = email,
                        UserName = email,
                        FirstName = firstName ?? "Google",
                        LastName = lastName ?? "User",
                        EmailConfirmed = true, // Google emails are verified
                        SecurityStamp = Guid.NewGuid().ToString(),
                        RefreshTokens = new List<RefreshTokens>()
                    };

                    var createResult = await _userManager.CreateAsync(user);

                    if (!createResult.Succeeded)
                    {
                        _logger.LogError("Failed to create user from Google login: {Errors}",
                            string.Join(", ", createResult.Errors.Select(e => e.Description)));
                        return BadRequest(new
                        {
                            message = "Failed to create user account",
                            errors = createResult.Errors.Select(e => e.Description)
                        });
                    }

                    // Add external login info
                    await _userManager.AddLoginAsync(user, new UserLoginInfo(
                        "Google",
                        googleId,
                        "Google"));

                    _logger.LogInformation("New user created from Google login: {Email}", email);
                }
                else
                {
                    // Check if this Google account is already linked
                    var existingLogin = await _userManager.FindByLoginAsync("Google", googleId);

                    if (existingLogin == null)
                    {
                        // Link this Google account to existing user
                        await _userManager.AddLoginAsync(user, new UserLoginInfo(
                            "Google",
                            googleId,
                            "Google"));
                    }

                    _logger.LogInformation("Existing user logged in via Google: {Email}", email);
                }

                // Generate JWT token using your existing auth service
                var authModel = await _authService.GenerateTokenForUserAsync(user);

                return Ok(new
                {
                    token = authModel.Token,
                    refreshToken = authModel.RefreshToken,
                    expiresOn = authModel.ExpiresOn,
                    email = authModel.Email,
                    userName = authModel.UserName,
                    roles = authModel.Roles,
                    message = "Google authentication successful"
                });
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogWarning("Invalid Google token: {Message}", ex.Message);
                return BadRequest(new { message = "Invalid Google token" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google authentication");
                return StatusCode(500, new { message = "An error occurred during authentication" });
            }
        }

        /// <summary>
        /// Verify Google ID token
        /// </summary>
        private async Task<GoogleJsonWebSignature.Payload> VerifyGoogleToken(string idToken)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _configuration["Authentication:Google:ClientId"] }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
                return payload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify Google token");
                return null;
            }
        }
    }

    /// <summary>
    /// Request model for Google Sign-In
    /// </summary>
    public class GoogleSignInRequest
    {
        public string IdToken { get; set; }
    }
}