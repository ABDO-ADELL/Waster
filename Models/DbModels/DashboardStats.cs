namespace Waster.Models.DbModels
{
    public class DashboardStats
    {
            public Guid Id { get; set; } = Guid.NewGuid();
            public int TotalDonations { get; set; }
            public int MealsServed { get; set; }  // Completed claims
            public int AvailablePosts { get; set; }
            public int ActiveUsers { get; set; }
            public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        
    }
}

