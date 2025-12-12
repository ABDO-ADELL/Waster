using Microsoft.EntityFrameworkCore;

namespace Waster.Helpers
{
    public static class BookmarkExtensions
    {
        // Check if a single post is bookmarked by a user
        public static async Task<bool> IsBookmarkedByUserAsync(this Post post,string userId,AppDbContext context)
        {
            return await context.BookMarks
                .AnyAsync(b => b.UserId == userId && b.PostId == post.Id);
        }

        // Get bookmark status for multiple posts in a single query
        // Returns a dictionary of PostId -> IsBookmarked
        public static async Task<Dictionary<Guid, bool>> GetBookmarkStatusAsync( this IEnumerable<Post> posts,string userId,AppDbContext context)
        {
            var postIds = posts.Select(p => p.Id).ToList();

            if (!postIds.Any())
                return new Dictionary<Guid, bool>();

            // Get all bookmarked post IDs in one query
            var bookmarkedPostIds = await context.BookMarks
                .Where(b => b.UserId == userId && postIds.Contains(b.PostId))
                .Select(b => b.PostId)
                .ToListAsync();

            // Create dictionary with all posts, marking bookmarked ones as true
            return postIds.ToDictionary(
                id => id,
                id => bookmarkedPostIds.Contains(id)
            );
        }

        // Enrich posts with bookmark status (alternative approach)
        // Returns tuples of (Post, IsBookmarked)
        public static async Task<List<(Post Post, bool IsBookmarked)>> WithBookmarkStatusAsync(
            this IEnumerable<Post> posts,
            string userId,
            AppDbContext context)
        {
            var postList = posts.ToList();
            var bookmarkStatus = await postList.GetBookmarkStatusAsync(userId, context);

            return postList.Select(p => (
                Post: p,
                IsBookmarked: bookmarkStatus.GetValueOrDefault(p.Id, false)
            )).ToList();
        }
    }
}