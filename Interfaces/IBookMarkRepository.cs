using Waster.DTOs;
using Waster.Models.DbModels;

namespace Waster.Interfaces
{
    public interface IBookMarkRepository
    {
        Task<(List<PostListItemDto>, int TotalCount)> GetUserBookmarksAsync(string userId, int pageSize, int pageNumber);

        Task<BookMark> AddBookmarkAsync(string userId, Guid postId);
        Task<bool> RemoveBookmarkAsync(string userId, Guid postId);
        Task<bool> IsBookmarkedAsync(string userId, Guid postId);
        Task<BookMark?> GetBookmarkAsync(string userId, Guid postId);
    }
}