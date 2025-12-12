using Waster.DTOs;
using Waster.Models.DbModels;

namespace Waster.Services
{
    public interface IBookMarkRepository
    {
        Task<(List<PostListItemDto>, int totalCount)> GetUserBookmarksAsync(string userId, int pageSize, int pageNumber);

        Task<BookMark> AddBookmarkAsync(string userId, Guid postId);
        Task<bool> RemoveBookmarkAsync(string userId, Guid postId);
        Task<bool> IsBookmarkedAsync(string userId, Guid postId);
        Task<BookMark?> GetBookmarkAsync(string userId, Guid postId);
    }
}