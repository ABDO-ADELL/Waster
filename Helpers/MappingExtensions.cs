using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Waster.DTOs;
using Waster.Services;

namespace Waster.Helpers
{
    public static class MappingExtensions
    {

        public static async Task<PostListItemDto>  ToListItemDto(this Post post,string userId , AppDbContext _context, List<Post> posts)
        {
            var bookmarkStatus =  await posts.GetBookmarkStatusAsync(userId, _context);

            return new PostListItemDto
            {
                Id = post.Id,
                Title = post.Title,
                Description = post.Description,
                Status = post.Status,
                Category = post.Category,
                ExpiresOn = post.ExpiresOn,
                ImageUrl = post.ImageUrl,
                IsBookmarked = bookmarkStatus.GetValueOrDefault(post.Id, false)

            };
        }

    }
    public static class ImageUrlHelper
    {
        public static string GetFullImageUrl(this HttpRequest request, string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return null;

            // If already a full URL, return as-is
            if (imageUrl.StartsWith("http://") || imageUrl.StartsWith("https://"))
                return imageUrl;

            // Build full URL
            var baseUrl = $"{request.Scheme}://{request.Host}";
            return $"{baseUrl}{imageUrl}";
     
        
        }
    }

}