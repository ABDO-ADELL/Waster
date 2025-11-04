    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Security.Claims;
    using Waster.Services;

    namespace Waster.Controllers
    {
        [Route("api/[controller]")]
        [ApiController]
        [Authorize]
        public class BrowseController(IUnitOfWork unitOfWork, ILogger<BrowseController> logger) : ControllerBase
        {
            private readonly IUnitOfWork _unitOfWork = unitOfWork;
            private readonly ILogger<BrowseController> _logger = logger;

            /// <summary>
            /// GET: api/browse/feed
            /// Get personalized feed with random available posts
            /// </summary>
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

                    // FIX: CS1061 - IUnitOfWork does not contain a definition for 'Posts'
                    // You must add a 'Posts' property to IUnitOfWork and its implementation, 
                    // and ensure it has a GetFeedAsync method.
                    // Example:
                    // public IPostRepository Posts { get; }
                    // If you have this, uncomment the next lines and specify the types.

                    // FIX: CS8130 - Cannot infer the type of implicitly-typed deconstruction variable 'items' and 'totalCount'
                    // You must specify the types explicitly.
                    // Example:
                    // (List<Post> items, int totalCount) = await _unitOfWork.Posts.GetFeedAsync(...);

                    // If you do not have IUnitOfWork.Posts or GetFeedAsync, you must add them.
                    // Otherwise, please provide the definition of IUnitOfWork and the repository.

                    // The following is a placeholder and will not compile until you add the missing members:
                    // (List<Post> items, int totalCount) = await _unitOfWork.Posts.GetFeedAsync(
                    //     userId,
                    //     pageSize,
                    //     category,
                    //     excludeOwn);

                    // If you provide the missing repository and method, this code will work.

                    // IDE0037: Member name can be simplified
                    // No changes needed here, as member names are already simple.

                    // Placeholder for demonstration:
                    var items = new List<object>(); // Replace 'object' with your Post type
                    var totalCount = 0; // Replace with actual count

                    if (totalCount == 0)
                    {
                        return Ok(new
                        {
                            items,
                            totalCount = 0,
                            pageSize,
                            message = "No posts available at the moment"
                        });
                    }

                    return Ok(new
                    {
                        items,
                        totalCount,
                        pageSize,
                        returnedCount = items.Count
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