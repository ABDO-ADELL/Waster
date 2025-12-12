using Microsoft.EntityFrameworkCore;
using Waster.DTOs;
using Waster.Helpers;
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

        public async Task<(List<PostListItemDto> , int totalCount)> GetUserBookmarksAsync(string userId, int pageSize, int pageNumber)
        {
            try
            {
                var query =  _context.BookMarks
                    .AsNoTracking()
                    .Where(b => b.UserId == userId)
                    .Include(b => b.Post)
                    .ThenInclude(p => p.AppUser)
                    .Select(b => b.Post)
                    .Where(p => !p.IsDeleted && p.IsValid);

                int totalCount = await query.CountAsync();

                var posts = await query.OrderByDescending(p => p.Created)
                        .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
                var bookmarkStatus = await posts.GetBookmarkStatusAsync(userId, _context);
                //var postDtos =  posts.Select(async p => p.ToListItemDto(userId , _context , posts));
                var postDtos = await Task.WhenAll(posts.Select(p => p.ToListItemDto(userId, _context, posts)));



                return (postDtos.ToList(), totalCount);
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
                    Id = Guid.NewGuid(),
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