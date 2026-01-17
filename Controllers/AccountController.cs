using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Waster.DTOs;
using Waster.Interfaces;
using Waster.Models;
using static Waster.Services.AccountService;
namespace Waster.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountController : ControllerBase
    {
        public readonly AppDbContext context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AccountController> _logger;
        private readonly IAccountService _accountService;

        public AccountController(AppDbContext Context, ILogger<AccountController> logger, UserManager<AppUser> userManager,IUnitOfWork unitOfWork, IAccountService accountService)
        {
            _unitOfWork = unitOfWork;
            context = Context;
            _userManager = userManager;
            _logger = logger;
            _accountService = accountService;
        }

        // DTOs are better than returning raw strings or entities

        [HttpGet("Profile")]
        public async Task<IActionResult> GetProfileAsync()
        {
            var result = await _accountService.GetProfileAsync();
            if (!result.Success)
                return NotFound(new { message = result.Message });
            return Ok(new
            {
                items = result.items,
            });

        }

        [HttpPut("Name")]
        public async Task<IActionResult> UpdateNameAsync([FromBody] UpdateNameRequest request)
        {
            var result = await _accountService.UpdateNameAsync(request);
            if (!result.Success)
                return BadRequest(new { message = result.Message });
            return Ok(new { message = result.Message });
        }


        [HttpPut("Bio")]
        public async Task<IActionResult> UpdateBioAsync([FromBody] UpdateBioRequest request)
        {
            var result = await _accountService.UpdateBioAsync(request);
            if (!result.Success)
                return BadRequest(new { message = result.Message });
            return Ok(new { message = result.Message });

        }

        [HttpPut("Password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var result = await _accountService.ChangePassword(dto);
            if (!result.Success)
                return BadRequest(new { message = result.Message });
            return Ok(new { message = result.Message });

        }


        [HttpPut("Email")]

        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailDto dto)
        {
            var result = await _accountService.ChangeEmail(dto);
            if (!result.Success)
                return BadRequest(new { message = result.Message });
            return Ok(new { message = result.Message });
        }




        [HttpPut("Location")]
        public async Task<IActionResult> UpdateLocationAsync([FromBody]UpdateLocation request)
        {
            var result = await _accountService.UpdateLocationAsync(request);
            if (!result.Success)
                return  BadRequest(new { message = result.Message });
            return Ok(result.Message);
        }



        [HttpPut("PhoneNumber")]
        public async Task<IActionResult> PhoneNumberAsync([FromBody] PhoneNumberDto request)
        {
            var result = await _accountService.PhoneNumberAsync(request);
            if (!result.Success)
                return BadRequest(new { message = result.Message });
            return Ok(new { message = result.Message });

        }


        [HttpGet("Posts")]
        public async Task<IActionResult> GetAllPosts([FromQuery]int pageNumber = 1 , [FromQuery]int pageSize = 10)
        {
            var result = await _accountService.GetAllPosts( pageNumber , pageSize);
            if (!result.Success)
                return BadRequest(new { message = result.Message });
            return Ok(result.items);
        }

        [HttpDelete("Account")]
        public async Task<IActionResult> DeleteAccountAsync([FromQuery]string password)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Invalid user identity.");
            var user = await context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            // Verify password
            var passwordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!passwordValid)
                return BadRequest(new { message = "Invalid password." });


            await _userManager.DeleteAsync(user);
            await context.SaveChangesAsync();
            return Ok(new { message = "Account deleted successfully." });
        }
    }



}

