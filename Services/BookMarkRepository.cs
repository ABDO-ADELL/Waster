using Microsoft.EntityFrameworkCore;
using Waster.DTOs;
using Waster.Helpers;
using Waster.Interfaces;
using Waster.Models.DbModels;


namespace Waster.Services
{
    public class BookMarkRepository : IBookMarkRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BookMarkRepository> _logger;

        public BookMarkRepository(AppDbContext context, ILogger<BookMarkRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(List<PostListItemDto>, int TotalCount)> GetUserBookmarksAsync(string userId,int pageSize,int pageNumber)
        {
            try
            {
                var query = _context.BookMarks
                    .AsNoTracking()
                    .Where(b => b.UserId == userId &&
                                b.Post != null &&
                                !b.Post.IsDeleted &&
                                b.Post.IsValid)
                    .Include(b => b.Post)
                        .ThenInclude(p => p.AppUser)
                    .OrderByDescending(b => b.Post.Created);

                var totalCount = await query.CountAsync();

                if (totalCount == 0)
                {
                    return (new List<PostListItemDto>(), 0);
                }
                

                var bookmarks = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => b.Post)
                    .ToListAsync();

                var postIds = bookmarks.Select(p => p.Id).ToList();

                //Get bookmark status for all posts 
                var bookmarkedPostIds = await _context.BookMarks
                    .Where(b => b.UserId == userId && postIds.Contains(b.PostId))
                    .Select(b => b.PostId)
                    .ToListAsync();

                var bookmarkedSet = new HashSet<Guid>(bookmarkedPostIds);
                var postDtos = bookmarks.Select(post => new PostListItemDto
                {
                    Id = post.Id,
                    Title = post.Title,
                    Description = post.Description,
                    Status = post.Status,
                    Category = post.Category,
                    ExpiresOn = post.ExpiresOn,
                    ImageUrl = post.ImageUrl,
                    IsBookmarked = bookmarkedSet.Contains(post.Id), // All should be true since these are from bookmarks
                    unit = post.Unit,  
                    quantity = post.Quantity,
                    pickupLocation = post.PickupLocation,
                    Created = post.Created,
                    Owner = new UserInfoDto
                    {
                        Id = post.AppUser.Id,
                        UserName = post.AppUser.FullName,
                        Email = post.AppUser.Email
                    }
                }).ToList();

                return (postDtos, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bookmarks for user {UserId}", userId);
                throw;
            }
        }
        public async Task<BookMark> AddBookmarkAsync(string userId, Guid postId)
        {
            try
            {
                var post = await _context.Posts
                    .Where(p => p.Id == postId && !p.IsDeleted && p.IsValid)
                    .FirstOrDefaultAsync();

                if (post == null)
                    throw new KeyNotFoundException("Post not found or is not available");

                var existingBookmark = await GetBookmarkAsync(userId, postId);
                if (existingBookmark != null)
                    throw new InvalidOperationException("Post is already bookmarked");

                var bookmark = new BookMark
                {
                    UserId = userId,
                    PostId = postId
                };

                await _context.BookMarks.AddAsync(bookmark);
                return bookmark;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding bookmark for user {UserId}, post {PostId}", userId, postId);
                throw;
            }
        }

        public async Task<bool> RemoveBookmarkAsync(string userId, Guid postId)
        {
            try
            {
                var bookmark = await GetBookmarkAsync(userId, postId);

                if (bookmark == null)
                    return false;

                _context.BookMarks.Remove(bookmark);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing bookmark for user {UserId}, post {PostId}", userId, postId);
                throw;
            }
        }

        public async Task<bool> IsBookmarkedAsync(string userId, Guid postId)
        {
            try
            {
                return await _context.BookMarks
                    .AnyAsync(b => b.UserId == userId && b.PostId == postId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking bookmark status for user {UserId}, post {PostId}", userId, postId);
                throw;
            }
        }

        public async Task<BookMark?> GetBookmarkAsync(string userId, Guid postId)
        {
            try
            {
                return await _context.BookMarks
                    .FirstOrDefaultAsync(b => b.UserId == userId && b.PostId == postId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bookmark for user {UserId}, post {PostId}", userId, postId);
                throw;
            }
        }
    }
}