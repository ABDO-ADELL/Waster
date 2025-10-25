using Waster.Models;
using Waster.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Waster.Services;

namespace Waster.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
public class AuthenticationController : Controller
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
            { return BadRequest(ModelState); }


            var result = await _auth.RegisterAsync(model);

            if (!result.IsAuthenticated)
            { return BadRequest(result.Message); }


            return Ok(new { token = result.Token });


        }
        [HttpPost("Login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
            { return BadRequest(ModelState); }


            var result = await _auth.LoginAsync(model);

            if (!result.IsAuthenticated)
            { return BadRequest(result.Message); }
            if (!string.IsNullOrEmpty(result.RefreshToken))
            {
                RefreshTokeinInCookie(result.RefreshToken, result.RefreshTokenExpiration);

            }
            return Ok(new { token = result.Token });
        }
        [HttpPost("AddRole")]
        public async Task<IActionResult> Role([FromBody] AddRole model)
        {
            if (!ModelState.IsValid)
            { return BadRequest(ModelState); }


            var result = await _auth.AddRoleAsync(model);

            if (!string.IsNullOrEmpty(result))
                return BadRequest(result);

            return Ok(model);
        }

        private void RefreshTokeinInCookie(string RefreshToken, DateTime expires)
        {
            var CookieOption = new CookieOptions
            {
                HttpOnly = true,
                Expires = expires.ToLocalTime(),
                SameSite = SameSiteMode.None,
                Secure = true

            };

            Response.Cookies.Append("RefreshToken", RefreshToken, CookieOption);


        }


        [HttpGet("RefreshToken")]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshtoken = Request.Cookies["RefreshToken"];
            var result = await _auth.RefreshTokenAsync(refreshtoken);
            if (!result.IsAuthenticated)
                return BadRequest();


            RefreshTokeinInCookie(result.RefreshToken, result.RefreshTokenExpiration);    

            return Ok(result);

        }
        [HttpPost("RevokeToken")]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeToken model)
        {

            var token = model.Token ?? Request.Cookies["RefreshToken"];
            if (string.IsNullOrEmpty(token))
                return BadRequest("Token Required");

            var result = await _auth.RevokeTokenAsync(token);

            if (!result)
                return BadRequest("Token Required");


            return Ok();
        }

    }
}

