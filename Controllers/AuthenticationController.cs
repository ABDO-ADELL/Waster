using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Waster.Models;
using Waster.Services;

namespace Waster.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthService _auth;

        public AuthenticationController(IAuthService auth)
        {
            _auth = auth;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _auth.RegisterAsync(model);

            if (!result.IsAuthenticated)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new
            {
                token = result.Token,
                refreshToken = result.RefreshToken,
                email = result.Email,
                userName = result.UserName,
                roles = result.Roles,
                expiresOn = result.ExpiresOn,
                message = result.Message
            });
        }

        [HttpPost("Login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _auth.LoginAsync(model);

            if (!result.IsAuthenticated)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new
            {
                token = result.Token,
                refreshToken = result.RefreshToken,
                email = result.Email,
                userName = result.UserName,
                roles = result.Roles,
                expiresOn = result.ExpiresOn,
                message = result.Message
            });
        }

        [HttpPost("AddRole")]
        public async Task<IActionResult> AddRole([FromBody] AddRole model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _auth.AddRoleAsync(model);

            if (result.Contains("Error") || result.Contains("does not exist") || result.Contains("already assigned"))
            {
                return BadRequest(new { message = result });
            }

            return Ok(new { message = result });
        }
        public record RefreshTokenRequest
        {
            public string RefreshToken { get; set; }
        }

        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest model)
        {
            if (string.IsNullOrEmpty(model?.RefreshToken))
            {
                return BadRequest(new { message = "Refresh token is required" });
            }

            var result = await _auth.RefreshTokenAsync(model.RefreshToken);

            if (!result.IsAuthenticated)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new
            {
                token = result.Token,
                refreshToken = result.RefreshToken,
                email = result.Email,
                userName = result.UserName,
                roles = result.Roles,
                expiresOn = result.ExpiresOn,
                message = result.Message
            });
        }

        [HttpPost("RevokeToken")]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeToken model)
        {
            if (string.IsNullOrEmpty(model?.Token))
            {
                return BadRequest(new { message = "Token is required" });
            }

            var result = await _auth.RevokeTokenAsync(model.Token);

            if (!result)
            {
                return BadRequest(new { message = "Token is invalid or already revoked" });
            }

            return Ok(new { message = "Token revoked successfully" });
        }
    }

}