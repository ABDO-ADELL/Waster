using Waster.DTOs;
using Waster.Models;
using Waster.Models.DbModels;
using Waster.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;

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
        public ClaimPostController(AppDbContext Context, IBaseReporesitory<Post> postRep, ILogger<ClaimPostController> logger , UserManager<AppUser> userManager,IClaimPostService claimPostService)
        {
            _postRepo = postRep;
          _context = Context;
            _logger = logger;
            _userManager = userManager;
            _claimPostService = claimPostService;
        }

            // POST: api/claims/post/{postId}
            [HttpPost]
            public async Task<IActionResult> ClaimPost([FromQuery]Guid postId)
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
                    return StatusCode(500, new { message = "An error occurred while claiming the post" });
                }
            }



            // GET: api/claims/my-claims
            [HttpGet("my-claims")]
            [Authorize]
            public async Task<IActionResult> GetMyClaims([FromQuery] string? status = null)
            {
                try
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                     var claims= await _claimPostService.GetUserClaims(userId, status);

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

        // GET: api/claims/post/{postId}/claims (Post owner views all claims)
        [HttpGet("Get-Post-claims")]
        public async Task<IActionResult> GetPostClaims([FromQuery]Guid postId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var post = await _context.Posts.FindAsync(postId);
                if (post == null)
                    return NotFound(new { message = "Post not found" });
                // Only post owner can view claims
                if (post.UserId != userId)
                    return Forbid("Only the post owner can see its claims");

                var response = await _claimPostService.GetPostClaims(userId, postId);
                if (!response.Any())
                {
                    return NotFound(new {message="this post has no claims"});
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting post claims");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

    
       [HttpPut("{claimId}/approve")]
            public async Task<IActionResult> ApproveClaim(Guid claimId)
            {
                try
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    var claim = await _context.ClaimPosts
                        .Include(c => c.Post)
                        .Include(c => c.Recipient)
                        .FirstOrDefaultAsync(c => c.Id == claimId);

                    if (claim == null)
                        return NotFound(new { message = "Claim not found" });

                    if (claim.Post.UserId != userId)
                        return Forbid();

                    if (claim.Status != "Pending")
                        return BadRequest(new { message = "Claim is not pending" });

                    claim.Status = "Approved";
                    claim.Post.Status = "Reserved";

                    // Reject other pending claims
                    var otherClaims = await _context.ClaimPosts
                        .Where(c => c.PostId == claim.PostId && c.Id != claimId && c.Status == "Pending")
                        .ToListAsync();

                    foreach (var other in otherClaims)
                        other.Status = "Rejected";

                    await _context.SaveChangesAsync();

                    var response = await _claimPostService.ClaimPostAsync(claimId, userId);

                    return Ok(new
                    {
                        message = "Claim approved successfully",
                        claim = response
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error approving claim");
                    return StatusCode(500, new { message = "An error occurred" });
                }
            }

            // PUT: api/claims/{claimId}/reject
            [HttpPut("{claimId}/reject")]
            public async Task<IActionResult> RejectClaim(Guid claimId)
            {
                try
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    var claim = await _context.ClaimPosts
                        .Include(c => c.Post)
                        .FirstOrDefaultAsync(c => c.Id == claimId);

                    if (claim == null)
                        return NotFound(new { message = "Claim not found" });

                    if (claim.Post.UserId != userId)
                        return Forbid();

                    if (claim.Status != "Pending")
                        return BadRequest(new { message = "Claim is not pending" });

                    claim.Status = "Rejected";

                    // Make post available if no pending claims
                    var hasPendingClaims = await _context.ClaimPosts
                        .AnyAsync(c => c.PostId == claim.PostId && c.Status == "Pending" && c.Id != claimId);

                    if (!hasPendingClaims)
                        claim.Post.Status = "Available";

                    await _context.SaveChangesAsync();

                    return Ok(new { message = "Claim rejected" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error rejecting claim");
                    return StatusCode(500, new { message = "An error occurred" });
                }
            }

            // DELETE: api/claims/{claimId}/cancel
            [HttpDelete("{claimId}/cancel")]
            public async Task<IActionResult> CancelClaim(Guid claimId)
            {
                try
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    var claim = await _context.ClaimPosts
                        .Include(c => c.Post)
                        .FirstOrDefaultAsync(c => c.Id == claimId);

                    if (claim == null)
                        return NotFound(new { message = "Claim not found" });

                    // Only recipient can cancel
                    if (claim.RecipientId != userId)
                        return Forbid();

                    // Can only cancel pending or approved claims
                    if (claim.Status == "Completed" || claim.Status == "Rejected")
                        return BadRequest(new { message = $"Cannot cancel a {claim.Status.ToLower()} claim" });

                    _context.ClaimPosts.Remove(claim);

                    // Make post available if no other pending claims
                    var hasPendingClaims = await _context.ClaimPosts
                        .AnyAsync(c => c.PostId == claim.PostId &&
                                     c.Id != claimId &&
                                     (c.Status == "Pending" || c.Status == "Approved"));

                    if (!hasPendingClaims)
                        claim.Post.Status = "Available";

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("User {UserId} cancelled claim {ClaimId}", userId, claimId);

                    return Ok(new { message = "Claim cancelled successfully" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cancelling claim");
                    return StatusCode(500, new { message = "An error occurred while cancelling the claim" });
                }
            }

            // PUT: api/claims/{claimId}/complete
            [HttpPut("{claimId}/complete")]
            public async Task<IActionResult> CompleteClaim(Guid claimId)
            {
                try
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    var claim = await _context.ClaimPosts
                        .Include(c => c.Post)
                        .FirstOrDefaultAsync(c => c.Id == claimId);

                    if (claim == null)
                        return NotFound(new { message = "Claim not found" });

                    // Either post owner or recipient can complete
                    if (claim.Post.UserId != userId && claim.RecipientId != userId)
                        return Forbid();

                    if (claim.Status != "Approved")
                        return BadRequest(new { message = "Claim must be approved first" });

                    claim.Status = "Completed";
                    claim.Post.Status = "Completed";

                    await _context.SaveChangesAsync();

                    return Ok(new { message = "Claim completed successfully" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error completing claim");
                    return StatusCode(500, new { message = "An error occurred" });
                }
            }

    }
}
