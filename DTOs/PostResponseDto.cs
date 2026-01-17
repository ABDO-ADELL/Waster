using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Waster.DTOs
{
    public class ResponseDto<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T? items { get; set; }
    }

    public class PostListItemDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string Category { get; set; }
        public DateTime ExpiresOn { get; set; }
        public string? ImageUrlRelative { get; set; }
        public bool? IsBookmarked { get; set; } 
        public string? ImageUrl { get; set; }
        public string quantity { get; set; }    
        public string unit { get; set; }
        public string pickupLocation { get; set; }
        public DateTime Created { get; set; }
        public UserInfoDto Owner { get; set; }
        public bool HasImage => !string.IsNullOrEmpty(ImageUrl);

    }
}