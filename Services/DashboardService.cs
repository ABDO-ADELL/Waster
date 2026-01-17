using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using Waster.Interfaces;
using Waster.Models.DTOs;
namespace Waster.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _accessor;
        private readonly IMapper _mapper;
        public DashboardService(AppDbContext context, IHttpContextAccessor accessor,IMapper mapper)
        {
            _context = context;
            _accessor = accessor;
            _mapper = mapper;
        }
        public async Task<DashboardResponseDto> DashboardStats()
        {
            var userId =_accessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return null;

            var result = await _context.dashboardStatus.FirstOrDefaultAsync(x => x.UserId == userId);
            if (result == null)
            {
                return new DashboardResponseDto { MealsServedInKG = 0.0, AvailablePosts = 0, TotalDonations = 0 };
            }

            return _mapper.Map<DashboardResponseDto>(result);
        }

        public async Task<DashboardResponseDto> CalcDashboardStatsAsync()
        {
            try
            {
                var userId = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                DashboardResponseDto dashboardResponseDto = new DashboardResponseDto();
                var userPosts = await _context.Posts
                    .Where(p => p.UserId == userId)
                    .ToListAsync();

                var AvailablePosts = userPosts.Where(p => p.Status.ToLower() == "available").Count();

                var myCompletedDonations = userPosts.Where(p => p.Status.ToLower() == "completed").Count();

                var TotalClaims = await _context.ClaimPosts.Where(c => c.RecipientId == userId).CountAsync();

                var myCompletedClaims = await _context.ClaimPosts.Where(c => c.RecipientId == userId && c.Status == "Completed").CountAsync();

                var myPendingClaims = await _context.ClaimPosts.Where(c => c.RecipientId == userId && c.Status == "Pending").CountAsync();
                double total = 0;
                foreach (var post in userPosts.Where(p => p.Status.ToLower() == "completed"))
                {
                    total += CalculateWeightInKg(post.Quantity, post.Unit);

                }
                //var kg = userPosts.Where(p => p.Status.ToLower() == "available").Sum(p => double.Parse(p.Quantity));

                return new DashboardResponseDto
                {
                    AvailablePosts = AvailablePosts,
                    TotalDonations = myCompletedDonations,
                    MealsServedInKG = total,
                    PendingClaims = myPendingClaims,
                    TotalClaims = TotalClaims,
                    Monthlygoals = 0
                };
            }
            catch (Exception ex)
            {
                return null;
            }


        }
        private double CalculateWeightInKg(string quantity, string unit)
        {
            if (!double.TryParse(quantity, out double numericQuantity))
                return 0;

            return unit.ToLower() switch
            {
                "kilogram" or "kg" => numericQuantity,
                "ton" or "tonne" => numericQuantity * 1000,
                "pound" or "lb" => numericQuantity * 0.453592,
                "gram" or "g" => numericQuantity / 1000,
                "pieces" or "items" => numericQuantity * 0.25,
                _ => numericQuantity
            };
        }

    }
}
