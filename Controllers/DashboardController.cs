using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Waster.Models.DTOs;

namespace Waster.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(AppDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/dashboard
        [HttpGet]
        public async Task<IActionResult> GetDashboard([FromQuery] DashboardFilterDto filter)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                // Apply date filters
                var startDate = filter.StartDate ?? DateTime.UtcNow.AddMonths(-1);
                var endDate = filter.EndDate ?? DateTime.UtcNow;

                // Base queries with filters
                var postsQuery = _context.Posts
                    .Where(p => p.Created >= startDate && p.Created <= endDate);

                var claimsQuery = _context.ClaimPosts
                    .Where(c => c.ClaimedAt >= startDate && c.ClaimedAt <= endDate);

                // Apply category filter if provided
                if (!string.IsNullOrEmpty(filter.Category))
                {
                    postsQuery = postsQuery.Where(p => p.Category == filter.Category);
                    claimsQuery = claimsQuery.Where(c => c.Post.Category == filter.Category);
                }

                // Apply status filter if provided
                if (!string.IsNullOrEmpty(filter.Status))
                {
                    postsQuery = postsQuery.Where(p => p.Status == filter.Status);
                }

                // Calculate stats
                var totalDonations = await postsQuery.CountAsync();
                var mealsServed = await claimsQuery.CountAsync(c => c.Status == "Completed");
                var availablePosts = await _context.Posts.CountAsync(p => p.Status == "Available");
                var activeUsers = await _context.Users.CountAsync(u => u.Posts.Any() || u.ClaimedPosts.Any());
                var pendingClaims = await _context.ClaimPosts.CountAsync(c => c.Status == "Pending");
                var completedToday = await _context.ClaimPosts
                    .CountAsync(c => c.Status == "Completed" &&
                                    c.ClaimedAt.Date == DateTime.UtcNow.Date);

                // Category breakdown
                var categoryStats = await postsQuery
                    .GroupBy(p => p.Category)
                    .Select(g => new CategoryStatsDto
                    {
                        Category = g.Key,
                        Count = g.Count(),
                        Percentage = totalDonations > 0 ? (g.Count() * 100 / totalDonations) : 0
                    })
                    .OrderByDescending(c => c.Count)
                    .ToListAsync();

                // Recent activity (last 10)
                var recentPosts = await postsQuery
                    .OrderByDescending(p => p.Created)
                    .Take(5)
                    .Select(p => new RecentActivityDto
                    {
                        Type = "donation",
                        Title = p.Title,
                        UserName = p.AppUser.UserName,
                        Timestamp = p.Created              })
                    .ToListAsync();

                var recentClaims = await claimsQuery
                    .OrderByDescending(c => c.ClaimedAt)
                    .Take(5)
                    .Select(c => new RecentActivityDto
                    {
                        Type = c.Status == "Completed" ? "completed" : "claim",
                        Title = c.Post.Title,
                        UserName = c.Recipient.UserName,
                        Timestamp = c.ClaimedAt
                    })
                    .ToListAsync();

                var recentActivity = recentPosts
                    .Concat(recentClaims)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(10)
                    .ToList();

                var dashboard = new DashboardResponseDto
                {
                    TotalDonations = totalDonations,
                    MealsServed = mealsServed,
                    AvailablePosts = availablePosts,
                    ActiveUsers = activeUsers,
                    PendingClaims = pendingClaims,
                    CompletedToday = completedToday,
                    CategoryBreakdown = categoryStats,
                    RecentActivity = recentActivity
                };

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return StatusCode(500, new { message = "An error occurred while fetching dashboard data" });
            }
        }

        // GET: api/dashboard/my-stats (User's personal stats)
        [HttpGet("my-stats")]
        public async Task<IActionResult> GetMyStats()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                var myDonations = await _context.Posts
                    .Where(p => p.UserId == userId)
                    .CountAsync();

                var myCompletedDonations = await _context.Posts
                    .Where(p => p.UserId == userId && p.Status == "Completed")
                    .CountAsync();

                var myClaims = await _context.ClaimPosts
                    .Where(c => c.RecipientId == userId)
                    .CountAsync();

                var myCompletedClaims = await _context.ClaimPosts
                    .Where(c => c.RecipientId == userId && c.Status == "Completed")
                    .CountAsync();

                var myPendingClaims = await _context.ClaimPosts
                    .Where(c => c.RecipientId == userId && c.Status == "Pending")
                    .CountAsync();

                return Ok(new
                {
                    totalDonations = myDonations,
                    completedDonations = myCompletedDonations,
                    totalClaims = myClaims,
                    completedClaims = myCompletedClaims,
                    pendingClaims = myPendingClaims,
                    impactScore = myCompletedDonations + myCompletedClaims
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user stats");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        // GET: api/dashboard/categories
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Posts
                    .Select(p => p.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }
    }
}


/*
---

## **4. Usage Examples**

### **Get overall dashboard:**
```
GET / api / dashboard
```

### **Filter by date range:**
```
GET / api / dashboard ? startDate = 2025 - 01 - 01 & endDate = 2025 - 10 - 25
```

### **Filter by category:**
```
GET / api / dashboard ? category = Bread
```

### **Filter by status:**
```
GET / api / dashboard ? status = Available
```

### **Combined filters:**
```
GET / api / dashboard ? startDate = 2025 - 10 - 01 & category = Vegetables & status = Completed
```

### **Get personal stats:**
```
GET / api / dashboard / my - stats
    */