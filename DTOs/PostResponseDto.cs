namespace Waster.DTOs
{
    public class PostResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Quantity { get; set; }
        public string Unit { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public string Status { get; set; }
        public string PickupLocation { get; set; }
        public DateTime ExpiresOn { get; set; }
        public DateTime Created { get; set; }
        public string? ImageUrl { get; set; }
        public bool HasImage => !string.IsNullOrEmpty(ImageUrl);
        public string? Notes { get; set; }

        // Only include if user is the owner
        public UserInfoDto? Owner { get; set; }
    }

    public class PostListItemDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string Category { get; set; }
        public DateTime ExpiresOn { get; set; }
        
        public string? ImageUrl { get; set; }
        public bool HasImage => !string.IsNullOrEmpty(ImageUrl);

    }
}