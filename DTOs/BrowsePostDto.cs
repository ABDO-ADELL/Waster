namespace Waster.DTOs
{
    public class BrowsePostDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Quantity { get; set; }
        public string Unit { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public string PickupLocation { get; set; }
        public DateTime ExpiresOn { get; set; }
        public DateTime Created { get; set; }

        public string? ImageUrl { get; set; }
        public bool HasImage => !string.IsNullOrEmpty(ImageUrl);

        // Bookmark status for current user
        public bool IsBookmarked { get; set; }

        // Optional: Hours until expiry (for expiring-soon endpoint)
        public int? HoursUntilExpiry { get; set; }

        // Post owner info
        public UserInfoDto Owner { get; set; }
    }
}