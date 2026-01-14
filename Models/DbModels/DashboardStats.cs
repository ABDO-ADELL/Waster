using System.ComponentModel.DataAnnotations.Schema;

namespace Waster.Models.DbModels
{
    public class DashboardStats
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int TotalDonations { get; set; } = 0;
        public double MealsServedInKG { get; set; } = 0;
        public int AvailablePosts { get; set; } = 0;
        public int TotalClaims { get; set; } = 0;
        public int PendingClaims { get; set; } = 0;
        public int Monthlygoals { get; set; } = 0;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public string UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public AppUser user { get; set; }


    }
}

