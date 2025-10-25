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

namespace Waster.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClaimPostController : ControllerBase
    {
        public AppDbContext _context { get; set; }
        private readonly IBaseReporesitory<Post> _postRepo;
        private readonly ILogger<ClaimPostController> _logger;
        private readonly UserManager<AppUser> _userManager;
        public ClaimPostController(AppDbContext Context, IBaseReporesitory<Post> postRep, ILogger<ClaimPostController> logger , UserManager<AppUser> userManager)
        {
            _postRepo = postRep;
          _context = Context;
            _logger = logger;
            _userManager = userManager;
        }




            // POST: api/claims/post/{postId}
            [HttpPost("post/{postId}")]
            [Authorize]
            public async Task<IActionResult> ClaimPost(Guid postId)
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

                    // Check if post exists
                    var post = await _context.Posts
                        .Include(p => p.AppUser)
                        .FirstOrDefaultAsync(p => p.Id == postId);

                    if (post == null)
                        return NotFound(new { message = "Post not found" });

                    if (post.Status != "Available")
                        return BadRequest(new { message = "Post is no longer available" });

                    if (post.UserId == userId)
                        return BadRequest(new { message = "You cannot claim your own post" });

                    // Check existing claims
                    var existingClaim = await _context.ClaimPosts
                        .FirstOrDefaultAsync(c => c.PostId == postId &&
                                                 c.RecipientId == userId &&
                                                 (c.Status == "Pending" || c.Status == "Approved"));

                    if (existingClaim != null)
                        return BadRequest(new { message = "You already have an active claim on this post" });

                    // Create claim
                    var claim = new ClaimPost
                    {
                        PostId = postId,
                        RecipientId = userId,
                        UserId = post.UserId,
                        Status = "Pending"
                    };

                    _context.ClaimPosts.Add(claim);
                    post.Status = "Claimed";
                    await _context.SaveChangesAsync();

                    // Return claim with both users' info
                    var response = await GetClaimDetailsAsync(claim.Id, userId);

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

            // GET: api/claims/{claimId}
            [HttpGet("{claimId}")]
            [Authorize]
            public async Task<IActionResult> GetClaimById(Guid claimId)
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

                    // Only recipient or post owner can view
                    if (claim.RecipientId != userId && claim.UserId != userId)
                        return Forbid();

                    var response = await GetClaimDetailsAsync(claimId, userId);
                    return Ok(response);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting claim {ClaimId}", claimId);
                    return StatusCode(500, new { message = "An error occurred" });
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

                    var query = _context.ClaimPosts
                        .Include(c => c.Post)
                            .ThenInclude(p => p.AppUser)
                        .Include(c => c.Recipient)
                        .Where(c => c.RecipientId == userId);

                    if (!string.IsNullOrEmpty(status))
                        query = query.Where(c => c.Status == status);

                    var claims = await query
                        .OrderByDescending(c => c.ClaimedAt)
                        .ToListAsync();

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
            [HttpGet("post/{postId}/claims")]
            [Authorize]
            public async Task<IActionResult> GetPostClaims(Guid postId)
            {
                try
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    var post = await _context.Posts.FindAsync(postId);
                    if (post == null)
                        return NotFound(new { message = "Post not found" });

                    // Only post owner can view claims
                    if (post.UserId != userId)
                        return Forbid();

                    var claims = await _context.ClaimPosts
                        .Include(c => c.Recipient)
                        .Include(c => c.Post)
                        .Where(c => c.PostId == postId)
                        .OrderByDescending(c => c.ClaimedAt)
                        .ToListAsync();

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
                            ExpiryDate = c.Post.ExpiresOn,
                        },
                        Recipient = new UserTransactionDto
                        {
                            Id = c.Recipient.Id,
                            UserName = c.Recipient.UserName,
                            FullName = c.Recipient.FullName,
                            Email = c.Recipient.Email,
                            PhoneNumber = c.Recipient.PhoneNumber,
                            Address = c.Recipient.Address,
                            City = c.Recipient.City,
                            ProfilePicture = c.Recipient.ProfilePictureUrl
                        }
                    });

                    return Ok(response);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting post claims");
                    return StatusCode(500, new { message = "An error occurred" });
                }
            }

            // PUT: api/claims/{claimId}/approve
            [HttpPut("{claimId}/approve")]
            [Authorize]
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

                    var response = await GetClaimDetailsAsync(claimId, userId);

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
            [Authorize]
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
            [Authorize]
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
            [Authorize]
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

            // Helper method to get full claim details
            private async Task<ClaimResponseDto> GetClaimDetailsAsync(Guid claimId, string requestingUserId)
            {
                var claim = await _context.ClaimPosts
                    .Include(c => c.Post)
                        .ThenInclude(p => p.AppUser)
                    .Include(c => c.Recipient)
                    .FirstOrDefaultAsync(c => c.Id == claimId);

            return new ClaimResponseDto
            {
                Id = claim.Id,
                PostId = claim.PostId,
                Status = claim.Status,
                ClaimedAt = claim.ClaimedAt,
                Post = new PostSummaryDto
                {
                    Id = claim.Post.Id,
                    Title = claim.Post.Title,
                    Description = claim.Post.Description,
                    Location = claim.Post.PickupLocation,
                    Status = claim.Post.Status,
                    ExpiryDate = claim.Post.ExpiresOn
                },
                Recipient = new UserTransactionDto
                {
                    Id = claim.Recipient.Id,
                    UserName = claim.Recipient.UserName,
                    FullName = claim.Recipient.FullName,
                    Email = claim.Recipient.Email,
                    PhoneNumber = claim.Recipient.PhoneNumber,
                    Address = claim.Recipient.Address,
                    City = claim.Recipient.City,
                    ProfilePicture = claim.Recipient.ProfilePictureUrl
                },
                PostOwner = new UserTransactionDto
                {
                    Id = claim.Post.AppUser.Id,
                    UserName = claim.Post.AppUser.UserName,
                    FullName = claim.Post.AppUser.FullName,
                    Email = claim.Post.AppUser.Email,
                    PhoneNumber = claim.Post.AppUser.PhoneNumber,
                    Address = claim.Post.AppUser.Address,
                    City = claim.Post.AppUser.City,
                    ProfilePicture = claim.Post.AppUser.ProfilePictureUrl
                }
            };
            }
    }
















        //[HttpPost("claim/{postId}")]
        //[Authorize]
        //public async Task<IActionResult> ClaimPost(Guid postId)
        //{
        //    try
        //    {
        //        // Get authenticated user
        //        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //        if (string.IsNullOrEmpty(userId))
        //            return Unauthorized(new { message = "User not authenticated" });

        //        // Check if post exists and is available
        //        var post = await _context.Posts
        //            .Include(p => p.AppUser)
        //            .FirstOrDefaultAsync(p => p.Id == postId);

        //        if (post == null)
        //            return NotFound(new { message = "Post not found" });

        //        // Check if post is still available
        //        if (post.Status != "Available")
        //            return BadRequest(new { message = "Post is no longer available" });

        //        // Prevent user from claiming their own post
        //        if (post.UserId == userId)
        //            return BadRequest(new { message = "You cannot claim your own post" });

        //        // Check if user already has a pending/approved claim on this post
        //        var existingClaim = await _context.ClaimPosts
        //            .FirstOrDefaultAsync(c => c.PostId == postId &&
        //                                     c.RecipientId == userId &&
        //                                     (c.Status == "Pending" || c.Status == "Approved"));

        //        if (existingClaim != null)
        //            return BadRequest(new { message = "You already have a claim on this post" });

        //        // Create claim
        //        var claim = new ClaimPost
        //        {
        //            PostId = postId,
        //            RecipientId = userId,
        //            UserId = post.UserId, // Store post owner for easy access
        //            Status = "Pending"
        //        };

        //        _context.ClaimPosts.Add(claim);

        //        // Update post status to "Claimed" (pending approval)
        //        post.Status = "pending approval";
        //        #region
        //        await _context.SaveChangesAsync();

        //        _logger.LogInformation("User {UserId} claimed post {PostId}", userId, postId);

        //        return Ok(new
        //        {
        //            message = "Claim submitted successfully. Waiting for approval.",
        //            claimId = claim.Id,
        //            status = claim.Status
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error claiming post {PostId}", postId);
        //        return StatusCode(500, new { message = "An error occurred while claiming the post" });
        //    }
        //    #endregion
        //}
        //[HttpPut("claims/{claimId}/approve")]
        //[Authorize]
        //public async Task<IActionResult> ApproveClaim(Guid claimId)
        //{
        //    try
        //    {
        //        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        //        var claim = await _context.ClaimPosts
        //            .Include(c => c.Post)
        //            .FirstOrDefaultAsync(c => c.Id == claimId);

        //        if (claim == null)
        //            return NotFound(new { message = "Claim not found" });

        //        // Only post owner can approve
        //        if (claim.Post.UserId != userId)
        //            return Forbid();

        //        if (claim.Status != "Pending")
        //            return BadRequest(new { message = "Claim is not pending" });

        //        claim.Status = "Approved";
        //        claim.Post.Status = "Reserved"; // Mark post as reserved

        //        // Reject all other pending claims on this post
        //        var otherClaims = await _context.ClaimPosts
        //            .Where(c => c.PostId == claim.PostId && c.Id != claimId && c.Status == "Pending")
        //            .ToListAsync();

        //        foreach (var other in otherClaims)
        //        {
        //            other.Status = "Rejected";
        //        }

        //        await _context.SaveChangesAsync();

        //        return Ok(new { message = "Claim approved successfully" });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error approving claim {ClaimId}", claimId);
        //        return StatusCode(500, new { message = "An error occurred" });
        //    }
        //}

        //[HttpPut("claims/{claimId}/reject")]
        //[Authorize]
        //public async Task<IActionResult> RejectClaim(Guid claimId)
        //{
        //    try
        //    {
        //        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        //        var claim = await _context.ClaimPosts
        //            .Include(c => c.Post)
        //            .FirstOrDefaultAsync(c => c.Id == claimId);

        //        if (claim == null)
        //            return NotFound(new { message = "Claim not found" });

        //        if (claim.Post.UserId != userId)
        //            return Forbid();

        //        if (claim.Status != "Pending")
        //            return BadRequest(new { message = "Claim is not pending" });

        //        claim.Status = "Rejected";

        //        // If no other pending claims, make post available again
        //        var hasPendingClaims = await _context.ClaimPosts
        //            .AnyAsync(c => c.PostId == claim.PostId && c.Status == "Pending" && c.Id != claimId);

        //        if (!hasPendingClaims)
        //        {
        //            claim.Post.Status = "Available";
        //        }

        //        await _context.SaveChangesAsync();

        //        return Ok(new { message = "Claim rejected" });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error rejecting claim");
        //        return StatusCode(500, new { message = "An error occurred" });
        //    }
        //}
        //[HttpGet("my-claims")]
        //[Authorize]
        //public async Task<IActionResult> GetMyClaims()
        //{
        //    try
        //    {
        //        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        //        var claims = await _context.ClaimPosts
        //            .Include(c => c.Post)
        //            .ThenInclude(p => p.AppUser)
        //            .Where(c => c.RecipientId == userId)
        //            .OrderByDescending(c => c.ClaimedAt)
        //            .Select(c => new
        //            {
        //                c.Id,
        //                c.Status,
        //                c.ClaimedAt,
        //                Post = new
        //                {
        //                    c.Post.Id,
        //                    c.Post.Title,
        //                    c.Post.Description,
        //                    c.Post.Location,
        //                    Owner = c.Post.User.UserName
        //                }
        //            })
        //            .ToListAsync();

        //        return Ok(claims);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error getting user claims");
        //        return StatusCode(500, new { message = "An error occurred" });
        //    }
        //}




}
