using Waster.Models.DbModels;

namespace Waster.Services
{
    public interface IBookMarkRepository
    {
        Task<List<Post>> GetUserBookmarksAsync(string userId);

        Task<BookMark> AddBookmarkAsync(string userId, Guid postId);
        Task<bool> RemoveBookmarkAsync(string userId, Guid postId);
        Task<bool> IsBookmarkedAsync(string userId, Guid postId);
        Task<BookMark?> GetBookmarkAsync(string userId, Guid postId);
    }
}