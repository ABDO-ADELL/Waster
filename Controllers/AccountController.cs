using Waster.DTOs;
using Waster.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Waster.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountController : ControllerBase
    {
        public readonly AppDbContext context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(AppDbContext Context, ILogger<AccountController> logger, UserManager<AppUser> userManager)
        {
            context = Context;
            _userManager = userManager;
            _logger = logger;
        }

        // DTOs are better than returning raw strings or entities
        public record UserProfileDto
        (string Id, string FirstName, string LastName, string FullName, string Email);

        [HttpGet("me")]
        public async Task<IActionResult> GetProfileAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Invalid user identity.");

            var dto = await context.Users
                .Where(u => u.Id == userId)
                .Select(u => new UserProfileDto(
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    $"{u.FirstName} {u.LastName}",
                    u.Email
                ))
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (dto == null)
                return NotFound("User not found.");

            return Ok(dto);
        }


        //[HttpGet("Name")]
        //public async Task<IActionResult> GetName() {
        //    var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    if (string.IsNullOrEmpty(id))  return Unauthorized("Invalid user identity.");

        //    var user = await context.Users.Where(i=>i.Id==id).FirstOrDefaultAsync();
        //    string Name = $"{user.FirstName} {user.LastName}";

        //    return Ok(Name);
        //}


        public record UpdateNameRequest(string FirstName, string LastName);

        [HttpPut("update-name")]
        public async Task<IActionResult> UpdateNameAsync([FromBody] UpdateNameRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
                return BadRequest("First name and last name are required.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Invalid user identity.");

            var user = await context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();

            await context.SaveChangesAsync();

            return Ok(new { message = "Name updated successfully." });
        }





        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Get current authenticated user
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated" });

                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                    return NotFound(new { message = "User not found" });

                // Change password using UserManager (handles hashing automatically)
                var result = await _userManager.ChangePasswordAsync(
                    user,
                    dto.CurrentPassword,
                    dto.NewPassword
                );

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserId} changed password successfully", userId);

                    return Ok(new
                    {
                        message = "Password changed successfully"
                    });
                }

                // Password change failed - return errors
                var errors = result.Errors.Select(e => e.Description).ToList();

                _logger.LogWarning("Password change failed for user {UserId}: {Errors}",
                    userId, string.Join(", ", errors));

                return BadRequest(new
                {
                    message = "Failed to change password",
                    errors = errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, new { message = "An error occurred while changing password" });
            }
        }





        [HttpPost("change-email")]  
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                #region additional verification
                // Get current authenticated user
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated" });

                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                    return NotFound(new { message = "User not found" });

                // Verify current password
                var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
                if (!passwordValid)
                    return BadRequest(new { message = "Invalid password" });
                #endregion

                // Generate token for new email
                var token = await _userManager.GenerateChangeEmailTokenAsync(user, dto.NewEmail);

                // Change email using the TOKEN 
                var result = await _userManager.ChangeEmailAsync(
                    user,
                    dto.NewEmail,
                    token
                    );

                if (result.Succeeded)
                {
                    //update the username if it matches the email
                    //await _userManager.SetUserNameAsync(user, dto.NewEmail);

                    _logger.LogInformation("User {UserId} changed email successfully", userId);

                    return Ok(new { message = "Email changed successfully" });
                }

                var errors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogWarning("Email change failed for user {UserId}: {Errors}",
                    userId, string.Join(", ", errors));

                return BadRequest(new
                {
                    message = "Failed to change email",
                    errors = errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing email");
                return StatusCode(500, new { message = "An error occurred while changing email" });
            }
        }



        public record UpdateLocation(string? City, string? State, string Address);

        [HttpPut("update-Location")]
        public async Task<IActionResult> UpdateLocationAsync([FromBody] UpdateLocation request)
        {
            if (string.IsNullOrWhiteSpace(request.Address))
                return BadRequest("Address is required.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Invalid user identity.");

            var user = await context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            user.Address = request.Address.Trim();
            if (!string.IsNullOrWhiteSpace(request.City)) user.City = request.City.Trim();
            if (!string.IsNullOrWhiteSpace(request.State)) user.State = request.State.Trim();
            await context.SaveChangesAsync();

            return Ok(new { message = "Location updated successfully." });
        }






        public record PhoneNumber(string PhoneNum);

        [HttpPut("update-PhoneNumber")]
        public async Task<IActionResult> PhoneNumberAsync([FromBody] PhoneNumber request)
        {
            if (string.IsNullOrWhiteSpace(request.PhoneNum))
                return BadRequest("PhoneNumber is required.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Invalid user identity.");

            var user = await context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            user.PhoneNumber = request.PhoneNum.Trim();

            await context.SaveChangesAsync();

            return Ok(new { message = "Location updated successfully." });
        }




    }



}

