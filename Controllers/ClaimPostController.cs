using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Waster.DTOs;
using Waster.Interfaces;
using Waster.Models;

namespace Waster.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ClaimPostController : ControllerBase
    {
        public AppDbContext _context { get; set; }
        private readonly IBaseReporesitory<Post> _postRepo;
        private readonly ILogger<ClaimPostController> _logger;
        private readonly UserManager<AppUser> _userManager;
        private readonly IClaimPostService _claimPostService;
        public ClaimPostController(AppDbContext Context, IBaseReporesitory<Post> postRep, ILogger<ClaimPostController> logger, UserManager<AppUser> userManager, IClaimPostService claimPostService)
        {
            _postRepo = postRep;
            _context = Context;
            _logger = logger;
            _userManager = userManager;
            _claimPostService = claimPostService;
        }

        [HttpPost]
        public async Task<IActionResult> ClaimPost([FromQuery] Guid postId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated" });

                // Get user with full details
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                // Validate user has required info
                if (string.IsNullOrEmpty(user.PhoneNumber) || string.IsNullOrEmpty(user.Address))
                {
                    return BadRequest(new
                    {
                        message = "Please complete your profile (phone and address) before claiming posts",
                        missingFields = new
                        {
                            phoneNumber = string.IsNullOrEmpty(user.PhoneNumber),
                            address = string.IsNullOrEmpty(user.Address)
                        }
                    });
                }

                var response = await _claimPostService.ClaimPostAsync(postId, userId);
                if (!response.IsSuccess)
                {
                    return BadRequest(new { message = response.Error });
                }
                _logger.LogInformation("User {UserId} claimed post {PostId}", userId, postId);


                return Ok(new
                {
                    message = "Claim submitted successfully. Waiting for approval.",
                    claim = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error claiming post {PostId}", postId);
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("claims")]
        public async Task<IActionResult> GetMyClaims([FromQuery] string? status = null)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var claims = await _claimPostService.GetUserClaims(userId, status);

                var response = claims.Select(c => new ClaimResponseDto
                {
                    Id = c.Id,
                    PostId = c.PostId,
                    Status = c.Status,
                    ClaimedAt = c.ClaimedAt,
                    Post = new PostSummaryDto
                    {
                        Id = c.Post.Id,
                        Title = c.Post.Title,
                        Description = c.Post.Description,
                        Location = c.Post.PickupLocation,
                        Status = c.Post.Status,
                        ExpiryDate = c.Post.ExpiresOn
                    },
                    PostOwner = new UserTransactionDto
                    {
                        Id = c.Post.UserId,
                        UserName = c.Post.AppUser.FirstName,
                        FullName = c.Post.AppUser.FullName,
                        Email = c.Post.AppUser.Email,
                        PhoneNumber = c.Post.AppUser.PhoneNumber,
                        Address = c.Post.AppUser.Address,
                        City = c.Post.AppUser.City,
                        ProfilePicture = c.Post.AppUser.ProfilePictureUrl,
                    }
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user claims");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        [HttpGet("Post-claims")]
        public async Task<IActionResult> GetPostClaims([FromQuery] Guid postId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var post = await _context.Posts.FindAsync(postId);
                // Only post owner can view claims
                if (post.UserId != userId)
                    return Unauthorized("Only the post owner can see its claims");
                if (post == null)
                    return NotFound(new { message = "Post not found" });

                var response = await _claimPostService.GetPostClaims(userId, postId);
                if (!response.Any())
                {
                    return NotFound(new { message = "this post has no claims" });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting post claims");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }
        [HttpPut("approve")]
        public async Task<IActionResult> ApproveClaim([FromQuery] Guid claimId)
        {
            var response = await _claimPostService.ApproveClaim(claimId);
            if (!response.Success)
            {
                return BadRequest(new { message = response.Message });
            }
            return Ok(new { message = response.Message });
        }

        [HttpPut("reject")]
        public async Task<IActionResult> RejectClaim([FromQuery] Guid claimId)
        {
            var response = await _claimPostService.RejectClaim(claimId);
            if (!response.Success)
            {
                return BadRequest(new { message = response.Message });
            }
            return Ok(new { message = response.Message });
        }

        [HttpDelete("Claim")]
        public async Task<IActionResult> CancelClaim([FromQuery] Guid claimId)
        {
            var response = await _claimPostService.CancelClaim(claimId);
            if (!response.Success)
            {
                return BadRequest(new { message = response.Message });
            }
            return Ok(new { message = response.Message });
        }

        [HttpPut("Complete")]
        public async Task<IActionResult> CompleteClaim([FromQuery] Guid claimId)
        {
            var response = await _claimPostService.CompleteClaim(claimId);
            if (!response.Success)
            {
                return BadRequest(new { message = response.Message });
            }
            return Ok(new { message = response.Message });
        }

    }
}
