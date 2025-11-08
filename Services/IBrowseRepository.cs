using Waster.DTOs;

namespace Waster.Services
{
    public interface IBrowseRepository
    {
        Task<(List<BrowsePostDto> Items, int TotalCount)> GetFeedAsync(
            string userId,
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

        Task<List<CategoryCountDto>> GetCategoriesAsync();

        Task<(List<BrowsePostDto> Items, int TotalCount)> SearchPostsAsync(
            string userId,
            string? searchTerm,
            string? category,
            int pageNumber,
            int pageSize);
    }
}