namespace Waster.Models.DTOs
{
    public class DashboardResponseDto
    {
        public int TotalDonations { get; set; }
        public int MealsServed { get; set; }
        public int AvailablePosts { get; set; }
        public int ActiveUsers { get; set; }
        public int PendingClaims { get; set; }
        public int CompletedToday { get; set; }

        public List<CategoryStatsDto> CategoryBreakdown { get; set; }
        public List<RecentActivityDto> RecentActivity { get; set; }
    }

    public class CategoryStatsDto
    {
        public string Category { get; set; }
        public int Count { get; set; }
        public int Percentage { get; set; }
    }

    public class RecentActivityDto
    {
        public string Type { get; set; }  // "donation", "claim", "completed"
        public string Title { get; set; }
        public string UserName { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class DashboardFilterDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; }
    }
}