using Microsoft.EntityFrameworkCore;
using Waster.DTOs;

namespace Waster.Services
{
    public class PostRepository : IPostRepository
    {
        private readonly AppDbContext _context;

        public PostRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(List<BrowsePostDto> Items, int TotalCount)> GetFeedAsync
            (string userId,int pageSize,string? category,  bool excludeOwn)
        {
            // Base query: Available, valid, non-deleted posts
            var query = _context.Posts
                .Include(p => p.AppUser)
                .Where(p => p.Status == "Available" &&
                           p.IsValid &&
                           !p.IsDeleted &&
                           p.ExpiresOn > DateTime.UtcNow); // Not expired

            // Exclude user's own posts
            if (excludeOwn)
            {
                query = query.Where(p => p.UserId != userId);
            }

            // Filter by category if provided
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Category == category);
            }

            // Get total count before randomization
            var totalCount = await query.CountAsync();

            if (totalCount == 0)
            {
                return (new List<BrowsePostDto>(), 0);
            }

            // Randomize using GUID (works on most databases)
            var randomPosts = await query
                .OrderBy(p => Guid.NewGuid())
                .Take(pageSize)
                .Select(p => new
                {
                    Post = p,
                    IsBookmarked = _context.BookMarks.Any(b =>
                        b.UserId == userId && b.PostId == p.Id)
                })
                .ToListAsync();

            // Map to DTOs
            var items = randomPosts.Select(item => new BrowsePostDto
            {
                Id = item.Post.Id,
                Title = item.Post.Title,
                Description = item.Post.Description,
                Quantity = item.Post.Quantity,
                Unit = item.Post.Unit,
                Type = item.Post.Type,
                Category = item.Post.Category,
                PickupLocation = item.Post.PickupLocation,
                ExpiresOn = item.Post.ExpiresOn,
                ImageUrl = item.Post.ImageUrl,
                Created = item.Post.Created,
                IsBookmarked = item.IsBookmarked,
                Owner = new UserInfoDto
                {
                    Id = item.Post.AppUser.Id,
                    UserName = item.Post.AppUser.UserName,
                    Email = item.Post.AppUser.Email
                }
            }).ToList();

            return (items, totalCount);
        }

    }
}
