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
        public string Status { get; set; }
        public DateTime ExpiresOn { get; set; }
        public DateTime Created { get; set; }

        public string? ImageUrl { get; set; }
        public bool HasImage => !string.IsNullOrEmpty(ImageUrl);

        public bool IsBookmarked { get; set; }

        public int? HoursUntilExpiry { get; set; }

        public UserInfoDto Owner { get; set; }


    }
    public class BrowseResponse
    {
            public int? TotalCount { get; set; }
            public int? totalPages { get; set; }
            public string? Message { get; set; }
            public bool Success { get; set; }   

    }
}