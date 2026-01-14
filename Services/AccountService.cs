using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using PhoneNumbers;
using System.Security.Claims;
using Waster.Controllers;
using Waster.DTOs;
using Waster.Interfaces;
using Waster.Models;

namespace Waster.Services
{
    public class AccountService : IAccountService
    {
        public readonly AppDbContext context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AccountController> _logger;
        private readonly IHttpContextAccessor _accessor;
        public AccountService(AppDbContext context, UserManager<AppUser> userManager, IUnitOfWork unitOfWork, ILogger<AccountController> logger, IHttpContextAccessor accessor)
        {
            this.context = context;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _accessor = accessor;
        }
        public record UserProfileDto
        (string Id, string FullName, string Email, string Address, string PhoneNumber, string Bio);
        public async Task<ResponseDto<object>> GetProfileAsync()
        {
            var userIdStr = _accessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            var dto = await context.Users
                .Where(u => u.Id == userIdStr)
                .Select(u => new UserProfileDto(
                    u.Id,
                    $"{u.FirstName} {u.LastName}",
                    u.Email,
                    u.Address,
                    u.PhoneNumber
                    , u.Bio

                )).AsNoTracking().FirstOrDefaultAsync();

            if (dto == null)
                return new ResponseDto<object> { Message = "User not found.", Success = false };

            return new ResponseDto<object> { Data = dto, Message = "sucsess", Success = true };


        }
        public class UpdateNameRequest()
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }

        }
        public async Task<ResponseDto<object>> UpdateNameAsync(UpdateNameRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
                return new ResponseDto<object> { Message = "First name and last name are required.", Success = false };

            var userId = _accessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return new ResponseDto<object> { Message = "Unauthorized", Success = false };

            var user = await context.Users.FindAsync(userId);
            if (user == null)
                return new ResponseDto<object> { Message = "User not found.", Success = false };

            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();

            await context.SaveChangesAsync();

            return new ResponseDto<object> { Message = "Name updated successfully.", Success = true };

        }
        public class UpdateBioRequest()
        {
            public string Bio { get; set; }
        }
        public async Task<ResponseDto<object>> UpdateBioAsync(UpdateBioRequest request)
        {

            var userId = _accessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return new ResponseDto<object> { Message = "Unauthorized", Success = false }
            ;

            var user = await context.Users.FindAsync(userId);
            if (user == null)
                return new ResponseDto<object> { Message = "User not found.", Success = false };

            user.Bio = request.Bio.Trim();

            await context.SaveChangesAsync();

            return new ResponseDto<object> { Message = "Bio updated successfully.", Success = true };
        }

        public async Task<ResponseDto<object>> ChangePassword(ChangePasswordDto dto)
        {

            try
            {
                // Get current authenticated user
                var userId = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return new ResponseDto<object> { Message = "User not authenticated", Success = true };

                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                    return new ResponseDto<object> { Message = "User not found", Success = true };

                // Change password using UserManager (handles hashing automatically)
                var result = await _userManager.ChangePasswordAsync(
                    user,
                    dto.CurrentPassword,
                    dto.NewPassword
                );
                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserId} changed password successfully", userId);

                    return new ResponseDto<object>
                    {
                        Success = true,
                        Message = "Password changed successfully"
                    };
                }

                // Password change failed - return errors
                var errors = result.Errors.Select(e => e.Description).ToList();

                _logger.LogWarning("Password change failed for user {UserId}: {Errors}",
                    userId, string.Join(", ", errors));

                return new ResponseDto<object> { Message = "Failed to change password", Success = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return new ResponseDto<object> { Message = "An error occurred while changing password", Success = false };
            }
        }

        public async Task<ResponseDto<object>> ChangeEmail(ChangeEmailDto dto)
        {
            try
            {
                #region additional verification
                // Get current authenticated user
                var userId = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return new ResponseDto<object> { Message = "User not authenticated", Success = false };

                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                    return new ResponseDto<object> { Message = "User not found", Success = false };

                // Verify current password
                var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
                if (!passwordValid)
                    return new ResponseDto<object> { Message = "Invalid password", Success = false };
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

                    return new ResponseDto<object> { Message = "Email changed successfully", Success = false };
                }

                var errors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogWarning("Email change failed for user {UserId}: {Errors}",
                    userId, string.Join(", ", errors));

                return new ResponseDto<object>
                {
                    Message = "Failed to change email" + errors,
                    Success = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing email");
                return new ResponseDto<object> { Message = "An error occurred while changing email", Success = false };
            }
        }

        public class UpdateLocation()
        {
            public string Address { get; set; }
        }
        public async Task<ResponseDto<object>> UpdateLocationAsync(UpdateLocation request)
        {
            if (string.IsNullOrWhiteSpace(request.Address))
                return new ResponseDto<object> { Message = "Address cannot be empty.", Success = false };

            var userId = _accessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return new ResponseDto<object> { Message = "Unauthorized", Success = false };

            var user = await context.Users.FindAsync(userId);
            if (user == null)
                return new ResponseDto<object> { Message = "User not found.", Success = false };

            user.Address = request.Address.Trim();
            await context.SaveChangesAsync();

            return new ResponseDto<object> { Message = "Address updated successfully.", Success = true };
        }

        public class PhoneNumberDto()
        {
            public string PhoneNum { get; set; }
        }
        public async Task<ResponseDto<object>> PhoneNumberAsync(PhoneNumberDto request)
        {

            // Validate phone number
            try
            {
                var phoneUtil = PhoneNumberUtil.GetInstance();
                var phoneNumber = phoneUtil.Parse(request.PhoneNum, "EG");

                if (!phoneUtil.IsValidNumber(phoneNumber))
                    return new ResponseDto<object> { Message = "Invalid phone number.", Success = false };
            }
            catch (NumberParseException)
            {
                return new ResponseDto<object> { Message = "Invalid phone number format.", Success = false };
            }


                if (string.IsNullOrWhiteSpace(request.PhoneNum))
                    return new ResponseDto<object> { Message = "Phone number cannot be empty.", Success = false };

                var userId = _accessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return new ResponseDto<object> { Message = "Unauthorized", Success = false };

                var user = await context.Users.FindAsync(userId);
                if (user == null)
                    return new ResponseDto<object> { Message = "User not found.", Success = false };


                var token = await _userManager.GenerateChangePhoneNumberTokenAsync(user, request.PhoneNum);

                await _userManager.ChangePhoneNumberAsync(user, request.PhoneNum, token);

                await context.SaveChangesAsync();

                return new ResponseDto<object> { Message = "Phone number updated successfully.", Success = true };
         }

        public async Task<ResponseDto<object>> GetAllPosts(int pageNumber ,int pageSize)
        {
            var userId = _accessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return new ResponseDto<object> { Message = "Unauthorized", Success = false };
            var (items, totalCount) = await _unitOfWork.Browse.GetMyPosts(userId, pageNumber, pageSize);
            if (!items.Any())
                return new ResponseDto<object>{Message= "No posts found for the specified user.", Success = true,

                    Data =(new
                {
                    items = items,
                    pageNumber = pageNumber,
                    pageSize = pageSize,
                    totalCount = totalCount,
                    totalPages = 0,
                    hasNext = false,
                    hasPrevious = pageNumber > 1,
                    message = "No posts found for the specified user."
                }) };

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return new ResponseDto<object>
            {
                Message = "Posts retrieved successfully.",
                Success = true,
                Data = (new
                {
                    items = items,
                    pageNumber = pageNumber,
                    pageSize = pageSize,
                    totalCount = totalCount,
                    totalPages = totalPages,
                    hasNext = pageNumber < totalPages,
                    hasPrevious = pageNumber > 1
                })
            };

        }




    }
}