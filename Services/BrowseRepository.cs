using Microsoft.EntityFrameworkCore;
using Waster.DTOs;

namespace Waster.Services
{
    public class BrowseRepository : IBrowseRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BrowseRepository> _logger;

        public BrowseRepository(AppDbContext context, ILogger<BrowseRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(List<BrowsePostDto> Items, int TotalCount)> GetFeedAsync(
            string userId,
            int pageSize,
            string? category,
            bool excludeOwn)
        {
            try
            {
                var query = _context.Posts
                    .Include(p => p.AppUser)
                    .Where(p => p.Status == "Available" &&
                               p.IsValid &&
                               !p.IsDeleted &&
                               p.ExpiresOn > DateTime.UtcNow);

                if (excludeOwn)
                    query = query.Where(p => p.UserId != userId);

                if (!string.IsNullOrEmpty(category))
                    query = query.Where(p => p.Category == category);

                var totalCount = await query.CountAsync();

                if (totalCount == 0)
                    return (new List<BrowsePostDto>(), 0);

                var randomPosts = await query
                    .OrderBy(p => Guid.NewGuid())
                    .Take(pageSize)
                    .ToListAsync();

                var items = await MapToPostDtosAsync(randomPosts, userId);

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feed for user {UserId}", userId);
                throw;
            }
        }

        public async Task<(List<BrowsePostDto> Items, int TotalCount)> GetExpiringSoonAsync(
            string userId,
            int hours,
            int pageSize)
        {
            try
            {
                var expiryThreshold = DateTime.UtcNow.AddHours(hours);

                var query = _context.Posts
                    .Include(p => p.AppUser)
                    .Where(p => p.Status == "Available" &&
                               p.IsValid &&
                               !p.IsDeleted &&
                               p.ExpiresOn > DateTime.UtcNow &&
                               p.ExpiresOn <= expiryThreshold &&
                               p.UserId != userId);

                var totalCount = await query.CountAsync();

                var posts = await query
                    .OrderBy(p => p.ExpiresOn)
                    .Take(pageSize)
                    .ToListAsync();

                var items = await MapToPostDtosAsync(posts, userId);

                foreach (var item in items)
                {
                    item.HoursUntilExpiry = (int)(item.ExpiresOn - DateTime.UtcNow).TotalHours;
                }

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expiring posts for user {UserId}", userId);
                throw;
            }
        }

        public async Task<(List<BrowsePostDto> Items, int TotalCount)> GetNearbyPostsAsync(
            string userId,
            string? userCity,
            int pageSize)
        {
            try
            {
                var query = _context.Posts
                    .Include(p => p.AppUser)
                    .Where(p => p.Status == "Available" &&
                               p.IsValid &&
                               !p.IsDeleted &&
                               p.ExpiresOn > DateTime.UtcNow &&
                               p.UserId != userId);

                if (!string.IsNullOrEmpty(userCity))
                {
                    query = query.Where(p => p.AppUser.City == userCity);
                }

                var totalCount = await query.CountAsync();

                var posts = await query
                    .OrderByDescending(p => p.Created)
                    .Take(pageSize)
                    .ToListAsync();

                var items = await MapToPostDtosAsync(posts, userId);

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting nearby posts for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<CategoryCountDto>> GetCategoriesAsync()
        {
            try
            {
                var categories = await _context.Posts
                    .Where(p => p.Status == "Available" &&
                               p.IsValid &&
                               !p.IsDeleted &&
                               p.ExpiresOn > DateTime.UtcNow)
                    .GroupBy(p => p.Category)
                    .Select(g => new CategoryCountDto
                    {
                        Category = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(c => c.Count)
                    .ToListAsync();

                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                throw;
            }
        }

        public async Task<(List<BrowsePostDto> Items, int TotalCount)> SearchPostsAsync(
            string userId,
            string? searchTerm,
            string? category,
            int pageNumber,
            int pageSize)
        {
            try
            {
                var query = _context.Posts
                    .Include(p => p.AppUser)
                    .Where(p => p.Status == "Available" &&
                               p.IsValid &&
                               !p.IsDeleted &&
                               p.ExpiresOn > DateTime.UtcNow &&
                               p.UserId != userId);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var search = searchTerm.ToLower();
                    query = query.Where(p =>
                        p.Title.ToLower().Contains(search) ||
                        p.Description.ToLower().Contains(search));
                }

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(p => p.Category == category);
                }

                var totalCount = await query.CountAsync();

                var posts = await query
                    .OrderByDescending(p => p.Created)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var items = await MapToPostDtosAsync(posts, userId);

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching posts for user {UserId}", userId);
                throw;
            }
        }

        private async Task<List<BrowsePostDto>> MapToPostDtosAsync(List<Post> posts, string userId)
        {
            if (!posts.Any())
                return new List<BrowsePostDto>();

            var postIds = posts.Select(p => p.Id).ToList();

            var bookmarkedPostIds = await _context.BookMarks
                .Where(b => b.UserId == userId && postIds.Contains(b.PostId))
                .Select(b => b.PostId)
                .ToListAsync();

            var items = posts.Select(p => new BrowsePostDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Quantity = p.Quantity,
                Unit = p.Unit,
                Type = p.Type,
                Category = p.Category,
                PickupLocation = p.PickupLocation,
                ExpiresOn = p.ExpiresOn,
                ImageUrl = p.ImageUrl,
                Created = p.Created,
                IsBookmarked = bookmarkedPostIds.Contains(p.Id),
                Owner = new UserInfoDto
                {
                    Id = p.AppUser.Id,
                    UserName = p.AppUser.UserName,
                    Email = p.AppUser.Email
                }
            }).ToList();

            return items;
        }
    }
}