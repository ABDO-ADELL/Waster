using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Security.Claims;
using Waster.DTOs;
using Waster.Helpers;
using Waster.Interfaces;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Waster.Services
{
    public class BrowseService : IBrowseService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BrowseService> _logger;
        private readonly IHttpContextAccessor _accessor;

        public BrowseService(AppDbContext context, ILogger<BrowseService> logger,IHttpContextAccessor accessor)
        {
            _context = context;
            _logger = logger;
            _accessor = accessor;
        }

        public async Task<(List<BrowsePostDto> Items, BrowseResponse count)> GetFeedAsync
            (int pageSize,string? category,bool excludeOwn)
        {
            var userId = _accessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);


            try
            {
                if (string.IsNullOrEmpty(userId))
                    return (new List<BrowsePostDto>(),new BrowseResponse { Message= "user must be authenticated" , Success = false});

                var query = _context.Posts
                    .Include(p => p.AppUser)
                    .Where(p => p.Status == "Available" &&
                               p.IsValid &&
                               !p.IsDeleted &&
                               p.ExpiresOn > DateTime.UtcNow).AsNoTracking();

                if (excludeOwn)
                    query = query.Where(p => p.UserId != userId);

                if (!string.IsNullOrEmpty(category))
                    query = query.Where(p => p.Category == category);

                var totalCount = await query.CountAsync();

                if (totalCount == 0)
                    return (new List<BrowsePostDto>(), new BrowseResponse { Success=false});

                var randomPosts = await query
                    .OrderBy(p => Guid.NewGuid())
                    .Take(pageSize)
                    .ToListAsync();

                var items = await MapToPostDtosAsync(randomPosts, userId);

                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                BrowseResponse response = new BrowseResponse{ Success= true,totalPages=totalPages , TotalCount=totalCount };

                return (items, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feed for user {UserId}", userId);
                throw;
            }
        }

        public async Task<(List<BrowsePostDto> Items, int TotalCount)>GetExpiringSoonAsync(
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
                               p.UserId != userId).AsNoTracking();

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

        public async Task<List<CategoryCountDto>> GetCategoriesAsync(string userId)
        {
            try
            {
                var categories = await _context.Posts
                    .Where(p => p.Status == "Available" &&
                               p.IsValid &&
                               !p.IsDeleted &&
                               p.ExpiresOn > DateTime.UtcNow && p.UserId!= userId)
                    .GroupBy(p => p.Category)
                    .Select(g => new CategoryCountDto
                    {
                        Category = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(c => c.Count)
                    .AsNoTracking().ToListAsync();

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

        public async Task<(List<BrowsePostDto>Items, int TotalCount)> GetMyPosts(string userId  ,int pageNumber, int pageSize)
        {

            var query =  _context.Posts
                .Include(p => p.AppUser)
                .Where(p => p.UserId == userId && !p.IsDeleted);
            var totalCount = await query.CountAsync();

            var posts = await query
                     .OrderByDescending(p => p.Created)
                     .Skip((pageNumber - 1) * pageSize)
                     .Take(pageSize)
                     .ToListAsync();
            var items = await MapToPostDtosAsync(posts, userId);
            return (items, totalCount);
        }
        public async Task<List<BrowsePostDto>> MapToPostDtosAsync(List<Post> posts, string userId)
        {

            if (!posts.Any())
                return new List<BrowsePostDto>();

            var postIds = posts.Select(p => p.Id).ToList();

            //var bookmarkedPostIds = await _context.BookMarks
            //    .Where(b => b.UserId == userId && postIds.Contains(b.PostId))
            //    .Select(b => b.PostId)
            //    .ToListAsync();

            var bookmarkStatus = await posts.GetBookmarkStatusAsync(userId, _context);


            var items = posts.Select(p => new BrowsePostDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Quantity = p.Quantity,
                Unit = p.Unit,
                Category = p.Category,
                PickupLocation = p.PickupLocation,
                ExpiresOn = p.ExpiresOn,
                ImageUrl = p.ImageUrl,
                Created = p.Created,
                Status = p.Status,
                //IsBookmarked = bookmarkedPostIds.Contains(p.Id),
                IsBookmarked = bookmarkStatus.GetValueOrDefault(p.Id, false), 
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