using Waster.DTOs;
using Waster.Services;

namespace Waster.Helpers
{
    public static class MappingExtensions
    {
        public static PostResponseDto ToResponseDto(this Post post, bool includeOwner = false)
        {


            var dto = new PostResponseDto
            {
                Id = post.Id,
                Title = post.Title,
                Description = post.Description,
                Quantity = post.Quantity,
                Unit = post.Unit,
                Category = post.Category,
                Status = post.Status,
                PickupLocation = post.PickupLocation,
                ExpiresOn = post.ExpiresOn,
                Created = post.Created,
                ImageUrl = post.ImageUrl,
            };

            if (includeOwner && post.AppUser != null)
            {
                dto.Owner = new UserInfoDto
                {
                    Id = post.AppUser.Id,
                    UserName = post.AppUser.UserName,
                    Email = post.AppUser.Email
                };
            }

            return dto;
        }

        public static PostListItemDto ToListItemDto(this Post post)
        {
            return new PostListItemDto
            {
                Id = post.Id,
                Title = post.Title,
                Description = post.Description,
                Status = post.Status,
                Category = post.Category,
                ExpiresOn = post.ExpiresOn,
                ImageUrl = post.ImageUrl, 
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