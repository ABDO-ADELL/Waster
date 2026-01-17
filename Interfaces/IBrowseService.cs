using Waster.DTOs;

namespace Waster.Interfaces
{
    public interface IBrowseService
    {
        Task<(List<BrowsePostDto> Items, BrowseResponse count)> GetFeedAsync(
            int pageSize,
            string? category,
            bool excludeOwn);

        Task<(List<BrowsePostDto> Items, int TotalCount)> GetExpiringSoonAsync(
            string userId,
            int hours,
            int pageSize);

        Task<(List<BrowsePostDto> Items, int TotalCount)> GetNearbyPostsAsync(
            string userId,
            string? userCity,
            int pageSize);

        Task<List<CategoryCountDto>> GetCategoriesAsync(string userId);

        Task<(List<BrowsePostDto> Items, int TotalCount)> SearchPostsAsync(
            string userId,
            string? searchTerm,
            string? category,
            int pageNumber,
            int pageSize);

        Task<(List<BrowsePostDto> Items, int TotalCount)> GetMyPosts(string userId, int pageNumber, int pageSize);

    }
}