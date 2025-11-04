using Waster.DTOs;
namespace Waster.Services
{
    public interface IPostRepository
    {
        Task<(List<BrowsePostDto> Items, int TotalCount)> GetFeedAsync(
            string userId,
            int pageSize,
            string? category,
            bool excludeOwn);

        //Task<List<BrowsePostDto>> GetNearbyPostsAsync(
        //    string userId,
        //    int pageSize,
        //    string? userCity);

        //Task<List<BrowsePostDto>> GetExpiringSoonAsync(
        //    string userId,
        //    int hours,
        //    int pageSize);

        //Task<List<CategoryCountDto>> GetCategoriesAsync();
    }
    public class CategoryCountDto
    {
        public string Category { get; set; }
        public int Count { get; set; }
    }
}
