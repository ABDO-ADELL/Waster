using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Waster.Services;

namespace Waster.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BrowseController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BrowseController> _logger;
        private readonly AppDbContext _context;

        public BrowseController(
            IUnitOfWork unitOfWork,
            ILogger<BrowseController> logger,
            AppDbContext context)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _context = context;
        }

        [HttpGet("feed")]
        public async Task<IActionResult> GetFeed(
            [FromQuery] int pageSize = 20,
            [FromQuery] string? category = null,
            [FromQuery] bool excludeOwn = true)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated" });

                var (items, totalCount) = await _unitOfWork.Browse.GetFeedAsync(
                    userId,
                    pageSize,
                    category,
                    excludeOwn);

                if (totalCount == 0)
                {
                    return Ok(new
                    {
                        items = items,
                        totalCount = 0,
                        pageSize = pageSize,
                        message = "No posts available at the moment"
                    });
                }
                return Ok(new
                {
                    items = items,
                    totalCount = totalCount,
                    pageSize = pageSize,
                    category = category
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting browse feed");
                return StatusCode(500, new { message = "An error occurred while fetching posts" });
            }
        }

        [HttpGet("expiring-soon")]
        public async Task<IActionResult> GetExpiringSoon(
            [FromQuery] int hours = 24,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated" });

                var (items, totalCount) = await _unitOfWork.Browse.GetExpiringSoonAsync(
                    userId,
                    hours,
                    pageSize);

                return Ok(new
                {
                    items = items,
                    totalCount = totalCount,
                    pageSize = pageSize,
                    hoursThreshold = hours,
                    message = totalCount == 0 ? "No posts expiring soon" : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expiring posts");
                return StatusCode(500, new { message = "An error occurred while fetching posts" });
            }
        }

        //[HttpGet("nearby")]
        //public async Task<IActionResult> GetNearby([FromQuery] int pageSize = 20)
        //{
        //    try
        //    {
        //        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //        if (string.IsNullOrEmpty(userId))
        //            return Unauthorized(new { message = "User not authenticated" });

        //        var user = await _context.Users
        //            .Where(u => u.Id == userId)
        //            .Select(u => new { u.City })
        //            .FirstOrDefaultAsync();

        //        var userCity = user?.City;

        //        var (items, totalCount) = await _unitOfWork.Browse.GetNearbyPostsAsync(
        //            userId,
        //            userCity,
        //            pageSize);

        //        return Ok(new
        //        {
        //            items = items,
        //            totalCount = totalCount,
        //            pageSize = pageSize,
        //            city = userCity,
        //            message = totalCount == 0 ? "No nearby posts found" : null
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error getting nearby posts");
        //        return StatusCode(500, new { message = "An error occurred while fetching posts" });
        //    }
        //}

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _unitOfWork.Browse.GetCategoriesAsync();

                return Ok(new
                {
                    categories = categories,
                    count = categories.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return StatusCode(500, new { message = "An error occurred while fetching categories" });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchPosts(
            [FromQuery] string? query = null,
            [FromQuery] string? category = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated" });

                var (items, totalCount) = await _unitOfWork.Browse.SearchPostsAsync(
                    userId,
                    query,
                    category,
                    pageNumber,
                    pageSize);

                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                return Ok(new
                {
                    items = items,
                    pageNumber = pageNumber,
                    pageSize = pageSize,
                    totalCount = totalCount,
                    totalPages = totalPages,
                    hasNext = pageNumber < totalPages,
                    hasPrevious = pageNumber > 1,
                    searchQuery = query,
                    category = category
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching posts");
                return StatusCode(500, new { message = "An error occurred while searching posts" });
            }
        }
    }
}