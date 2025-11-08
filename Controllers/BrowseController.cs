using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Waster.Services;
using Waster.Models;

namespace Waster.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BrowseController(IUnitOfWork unitOfWork, ILogger<BrowseController> logger) : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ILogger<BrowseController> _logger = logger;

        [HttpGet("feed")]
        public async Task<IActionResult> GetFeed([FromQuery] int pageSize = 20, [FromQuery] string? category = null, [FromQuery] bool excludeOwn = true)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated" });

                var result = await _unitOfWork.Posts.GetFeedAsync(
                    userId,
                    pageSize,
                    category,
                    excludeOwn);

                var (items, totalCount) = result;

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
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting browse feed");
                return StatusCode(500, new { message = "An error occurred while fetching posts" });
            }
        }
    }
}