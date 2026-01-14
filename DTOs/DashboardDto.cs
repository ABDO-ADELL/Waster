using AutoMapper;
using System.ComponentModel.DataAnnotations.Schema;
using Waster.Models.DbModels;

namespace Waster.Models.DTOs
{
    public class DashboardResponseDto
    {
        public int TotalDonations { get; set; } = 0;
        public double MealsServedInKG { get; set; } = 0;
        public int AvailablePosts { get; set; } = 0;
        public int TotalClaims { get; set; } = 0;
        public int PendingClaims { get; set; } = 0;
        public int Monthlygoals { get; set; } = 0;
    }
    public class DashboardProfile :Profile
    {
        public DashboardProfile()
        {
            CreateMap<DashboardStats, DashboardResponseDto>().ReverseMap();
        }
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