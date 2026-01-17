using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol;
using System.Security.Claims;
using System.Threading.Tasks;
using Waster.DTOs;
using Waster.Interfaces;
using Waster.Models.DbModels;

namespace Waster.Services
{
    public class ClaimPostService: IClaimPostService
    {

        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _Accessor;
        private readonly ILogger<ClaimPostService> _logger;
        public ClaimPostService(AppDbContext context , IHttpContextAccessor accessor, ILogger<ClaimPostService> logger)
        {
            _context = context;
            _Accessor = accessor;
            _logger = logger;
        }
        public async Task<Result<ClaimResponseDto>> ClaimPostAsync(Guid postId, string userId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
                return Result<ClaimResponseDto>.Failure("Post not found");

            var validationResult = await ValidateClaimAsync(post, postId, userId);
            if (!validationResult.IsSuccess)
                return validationResult;
            var dashboard = await _context.dashboardStatus.FirstOrDefaultAsync(u => u.UserId == userId);
            dashboard.PendingClaims += 1;
            await _context.SaveChangesAsync();
            return await CreateClaimAsync(post, postId, userId);
        }

        private async Task<Result<ClaimResponseDto>> ValidateClaimAsync(Post post, Guid postId, string userId)
        {
            if (post.UserId == userId)
                return Result<ClaimResponseDto>.Failure("You cannot claim your own post");

            if (post.Status != "Available")
                return Result<ClaimResponseDto>.Failure("Post is no longer available");

            var hasActiveClaim = await _context.ClaimPosts
                .AnyAsync(c => c.PostId == postId &&
                              c.RecipientId == userId &&
                              (c.Status == "Pending" || c.Status == "Approved"));

            if (hasActiveClaim)
                return Result<ClaimResponseDto>.Failure("You already have an active claim on this post");

            return Result<ClaimResponseDto>.Success(null);
        }

        private async Task<Result<ClaimResponseDto>> CreateClaimAsync(Post post, Guid postId, string userId)
        {
            var claim = new ClaimPost
            {
                PostId = postId,
                RecipientId = userId,
                UserId = post.UserId,
                Status = "Pending"
            };
            _context.ClaimPosts.Add(claim);
            await _context.SaveChangesAsync();

            var response = await GetClaimDetailsAsync(claim, post, userId);
            return Result<ClaimResponseDto>.Success(response);
        }

        public async Task<List<ClaimPost>> GetUserClaims(string userId, string? status = null)
        {
            //var claims = await _context.ClaimPosts
            //    .Where(c => c.UserId == userId.ToString())
            //    .ToListAsync();
            var query = Enumerable.Empty<ClaimPost>().AsQueryable();
            if (!string.IsNullOrEmpty(status))
            {
                 query = _context.ClaimPosts
                .Include(c => c.Post)
                .ThenInclude(p => p.AppUser)
                .Include(c => c.Recipient)
                .Where(c => c.RecipientId == userId&&c.Status==status);

            }else
            {
                 query = _context.ClaimPosts
                .Include(c => c.Post)
                .ThenInclude(p => p.AppUser)
                .Include(c => c.Recipient)
                .Where(c => c.RecipientId == userId);
            }

            var claims = await query
                .ToListAsync();
            return claims;
        }
        //public  async Task<bool> UpdateDashboard(string ownerId , string recipientId , double quantity)
        //{
        //    var ownerDashboard = _context.dashboardStatus.FirstOrDefault(d => d.UserId.ToString() == ownerId);
        //    if (ownerDashboard != null)
        //    {
        //        ownerDashboard.MealsServed += quantity;
        //        ownerDashboard.TotalDonations += 1;
        //        ownerDashboard.LastUpdated = DateTime.UtcNow;
        //        _context.dashboardStatus.Update(ownerDashboard);
        //    }
        //    var recipientDashboard = _context.dashboardStatus.FirstOrDefault(d => d.UserId.ToString() == recipientId);
        //    if (recipientDashboard != null)
        //    {
        //        recipientDashboard.MealsServed += quantity;
        //        recipientDashboard.TotalDonations += 1;
        //        recipientDashboard.LastUpdated = DateTime.UtcNow;
        //        _context.dashboardStatus.Update(recipientDashboard);
        //    }
        //    return  true;
        //}


        //get all claims for a post (owner)
        public async Task<List<ClaimResponseDto>> GetPostClaims( string ownerId , Guid postId)
        {


            var claims = await _context.ClaimPosts
                .Include(c => c.Recipient)
                .Include(c => c.Post)
                .Where(c => c.PostId == postId && c.Status!="Cancelled")
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
            }).ToList();
            return response;
        }


        // Helper method to get full claim details
        public async Task<ClaimResponseDto> GetClaimDetailsAsync( ClaimPost claim, Post post, string userId)
        {
            var owner = await _context.Users.FindAsync(post.UserId);
            var Recipient = await _context.Users.FindAsync(userId);
            return new ClaimResponseDto
            {
                Id = claim.Id,
                PostId = claim.PostId,
                Status = claim.Status,
                ClaimedAt = claim.ClaimedAt,
                Post = new PostSummaryDto
                {
                    Id = post.Id,
                    Title = post.Title,
                    Description = post.Description,
                    Location = post.PickupLocation,
                    Status = post.Status,
                    ExpiryDate = post.ExpiresOn
                },
                Recipient = new UserTransactionDto
                {
                    Id = Recipient.Id,
                    UserName = Recipient.UserName,
                    FullName = Recipient.FullName,
                    Email = Recipient.Email,
                    PhoneNumber = Recipient.PhoneNumber,
                    Address = Recipient.Address,
                    City = Recipient.City,
                },
                PostOwner = new UserTransactionDto
                {
                    Id = owner.Id,
                    UserName = owner.UserName,
                    FullName = owner.FullName,
                    Email = owner.Email,
                    PhoneNumber = owner.PhoneNumber,
                    Address = owner.Address,
                    City = owner.City,
                }
            };
        }
        public async Task<ResponseDto<object>> ApproveClaim(Guid claimId)
        {
            try
            {
                var userId = _Accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var claim = await _context.ClaimPosts
                    .Include(c => c.Post)
                    .Include(c => c.Recipient)
                    .FirstOrDefaultAsync(c => c.Id == claimId);

                if (claim == null)
                    return new ResponseDto<object> { Success = false, Message = "Claim not found"  };

                if (claim.Post.UserId != userId)
                    return new ResponseDto<object> { Success = false, Message = "Unauthorized. Only post owner can approve claims" };

                if (claim.Status != "Pending")
                    return new ResponseDto<object> { Success = false, Message = "Only pending claims can be approved" };

                claim.Status = "Approved";
                claim.Post.Status = "Reserved";

                // Reject other pending claims
                var otherClaims = await _context.ClaimPosts
                    .Where(c => c.PostId == claim.PostId && c.Id != claimId && c.Status == "Pending")
                    .ToListAsync();

                foreach (var other in otherClaims)
                    other.Status = "Rejected";


                await _context.SaveChangesAsync();

                //var response = await _claimPostService.ClaimPostAsync(claimId, userId);

                return new ResponseDto<object>
                {
                    Success = true,
                    Message = "Claim approved successfully",
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving claim");
                return new ResponseDto<object> { Success=false,Message = "An error occurred(500)" };
            }


        }
        public async Task<ResponseDto<object>> CancelClaim(Guid claimId)
        {
            try
            {
                var userId = _Accessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);


                var claim = await _context.ClaimPosts
                    .Include(c => c.Post)
                    .FirstOrDefaultAsync(c => c.Id == claimId);

                if (claim == null)
                    return new ResponseDto<object> { Message = "Claim not found" ,Success=false};

                if (claim.RecipientId != userId)
                   return new ResponseDto<object> { Message = "Only recipient can cancel", Success = false };


                // Can only cancel pending or approved claims
                if (claim.Status == "Completed" || claim.Status == "Rejected")
                    return new ResponseDto<object> { Message = $"Cannot cancel a {claim.Status.ToLower()} claim" , Success=false};


                // Make post available if no other pending claims
                var hasPendingClaims = await _context.ClaimPosts
                    .AnyAsync(c => c.PostId == claim.PostId &&
                                 c.Id != claimId &&
                                 (c.Status == "Pending" || c.Status == "Approved"));

                if (!hasPendingClaims)
                    claim.Post.Status = "Available";

                claim.Status = "Cancelled";
                _context.ClaimPosts.Remove(claim);
                var dashboard = await _context.dashboardStatus.FirstOrDefaultAsync(u => u.UserId == claim.RecipientId);
                dashboard.PendingClaims -= 1;
                await _context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} cancelled claim {ClaimId}", userId, claimId);


                return new ResponseDto<object> { Message = "Claim cancelled successfully" , Success=true};
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling claim");
                return new ResponseDto<object> { Success=false, Message = "An error occurred while cancelling the claim" };
            }
        }
        public async Task<ResponseDto<object>> RejectClaim( Guid claimId)
        {
            try
            {
                var userId = _Accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var claim = await _context.ClaimPosts
                    .Include(c => c.Post)
                    .FirstOrDefaultAsync(c => c.Id == claimId);

                if (claim == null)
                    return new ResponseDto<object> { Success=false, Message = "Claim not found" };

                if (claim.Post.UserId != userId)
                    return new ResponseDto<object> { Success=false, Message = "Unauthorized. Only post owner can reject claims" };

                if (claim.Status != "Pending")
                    return new ResponseDto<object> { Success=false, Message = "Only pending claims can be rejected" };

                claim.Status = "Rejected";

                // Make post available if no pending claims
                var hasPendingClaims = await _context.ClaimPosts
                    .AnyAsync(c => c.PostId == claim.PostId && c.Status == "Pending" && c.Id != claimId);

                if (!hasPendingClaims)
                    claim.Post.Status = "Available";

                var recipientId = claim.RecipientId;
                var dashboard = await _context.dashboardStatus.FirstOrDefaultAsync(d => d.UserId == recipientId);
                dashboard.PendingClaims -= 1;
               
                await _context.SaveChangesAsync();

                return new ResponseDto<object> { Success=true,Message = "Claim rejected Successfully" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting claim");
                return new ResponseDto<object> { Success=false,Message = "An error occurred(500)" };
            }


        }

        public async Task<ResponseDto<object>> CompleteClaim(Guid claimId)
        {
            try
            {
                var userId = _Accessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                var claim = await _context.ClaimPosts
                    .Include(c => c.Post)
                    .FirstOrDefaultAsync(c => c.Id == claimId);

                if (claim == null)
                    return new ResponseDto<object> { Success = false, Message = "Claim not found" };

                if (claim.Post.UserId != userId && claim.RecipientId != userId)
                    return new ResponseDto<object> { Success = false, Message = "Unauthorized. Either post owner or recipient can complete" };

                if (claim.Status == "Completed")
                    return new ResponseDto<object> { Success = false, Message = "Claim is already completed" };

                if (claim.Status != "Approved")
                    return new ResponseDto<object> { Success = false, Message = "Claim must be approved first" };

                claim.Status = "Completed";
                claim.Post.Status = "Completed";

                double weightInKg = CalculateWeightInKg(claim.Post.Quantity, claim.Post.Unit);

                var ownerDashboard = await _context.dashboardStatus
                    .FirstOrDefaultAsync(d => d.UserId == claim.Post.UserId);

                if (ownerDashboard.AvailablePosts > 0)
                    ownerDashboard.AvailablePosts -= 1;
                ownerDashboard.MealsServedInKG += weightInKg;
                ownerDashboard.TotalDonations += 1;
                ownerDashboard.LastUpdated = DateTime.UtcNow;

                var recipientDashboard = await _context.dashboardStatus
                    .FirstOrDefaultAsync(d => d.UserId == claim.RecipientId);

                recipientDashboard.MealsServedInKG += weightInKg;
                recipientDashboard.TotalClaims += 1;
                if (recipientDashboard.PendingClaims > 0)
                    recipientDashboard.PendingClaims -= 1;
                recipientDashboard.LastUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Claim {ClaimId} completed by user {UserId}", claimId, userId);

                return new ResponseDto<object> { Success = true, Message = "Claim completed successfully" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing claim {ClaimId}", claimId);
                return new ResponseDto<object> { Success = false, Message = "An error occurred while completing the claim" };
            }
        }

        private double CalculateWeightInKg(string quantity, string unit)
        {
            if (!double.TryParse(quantity, out double numericQuantity))
                return 0;

            return unit.ToLower() switch
            {
                "kilogram" or "kg" => numericQuantity,
                "ton" or "tonne" => numericQuantity * 1000,
                "pound" or "lb" => numericQuantity * 0.453592,
                "gram" or "g" => numericQuantity / 1000,
                "pieces" or "items" => numericQuantity * 0.25,
                _ => numericQuantity
            };
        }
    }
}
