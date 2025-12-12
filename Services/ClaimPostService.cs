using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Waster.DTOs;
using Waster.Models.DbModels;

namespace Waster.Services
{
    public class ClaimPostService: IClaimPostService
    {

        public readonly AppDbContext _context;
        public ClaimPostService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<ClaimResponseDto> ClaimPostAsync(Guid postId, string userId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
            {
                throw new Exception("Post not found");
            }

            // Check for existing active claims by the user on the same post
            var existingClaim = await _context.ClaimPosts
             .FirstOrDefaultAsync(c => c.PostId == postId &&
                             c.RecipientId == userId &&
                             (c.Status == "Pending" || c.Status == "Approved"));

            if (existingClaim != null)
                 throw new Exception("you already have claims on this post");

            // Validate post availability and ownership
            if (post == null)
                throw new Exception("Post not found" );

            if (post.Status != "Available")
                throw new Exception( "Post is no longer available" );

            if (post.UserId == userId)
                throw new Exception( "You cannot claim your own post" );


            // Create new claim
            var claim = new ClaimPost
            {
                PostId = postId,
                RecipientId = userId,
                UserId = post.UserId,
                Status = "Pending"
            };
            var response = await GetClaimDetailsAsync(claim, post, userId);


            _context.ClaimPosts.Add(claim);
            await _context.SaveChangesAsync();

            return response ;

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

        //get all claims for a post (owner)
        public async Task<List<ClaimResponseDto>> GetPostClaims( string ownerId , Guid postId)
        {


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
                Id = Guid.NewGuid(),
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




    }
}
