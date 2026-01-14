using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Security.Claims;
using Waster.Helpers;
using Waster.Interfaces;

namespace Waster.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BookMarksController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BookMarksController> _logger;

        public BookMarksController(
            IUnitOfWork unitOfWork,
            ILogger<BookMarksController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        [HttpGet("Bookmarks")]
        public async Task<IActionResult> GetBookmarks([FromQuery]int pageNumber = 1 , [FromQuery] int pageSize=10)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated" });

                var (postDtos, totalCount) = await _unitOfWork.BookMarks.GetUserBookmarksAsync(userId , pageSize, pageNumber);

                if (totalCount == 0)
                {
                    return Ok(new
                    {
                        items = postDtos,
                        totalCount = 0,
                        pageSize = pageSize,
                        message = "No posts available at the moment"
                    });
                }

                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);


                return Ok(new
                {
                    items = postDtos,
                    count = postDtos.Count,
                    totalCount = totalCount,
                    pageSize = pageSize,
                    pageNumber = pageNumber,
                    totalPages = totalPages,
                    hasNext = pageNumber < totalPages,
                    hasPrevious = pageNumber > 1

                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bookmarks");
                return StatusCode(500, new { message = "An error occurred while retrieving bookmarks" });
            }
        }

        [HttpPost("{postId}")]
        public async Task<IActionResult> AddBookmark([FromQuery]Guid PostId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated" });

                var bookmark = await _unitOfWork.BookMarks.AddBookmarkAsync(userId, PostId);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("User {UserId} bookmarked post {PostId}", userId, PostId);

                return CreatedAtAction(
                    nameof(GetBookmarks),
                    new { id = bookmark.Id },
                    new
                    {
                        message = "Post bookmarked successfully",
                        bookmarkId = bookmark.Id,
                        postId = bookmark.PostId
                    });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding bookmark for post {PostId}", PostId);
                return StatusCode(500, new { message = "An error occurred while adding bookmark" });
            }
        }

        [HttpDelete("Bookmark")]
        public async Task<IActionResult> RemoveBookmark([FromQuery]Guid postId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated" });

                var result = await _unitOfWork.BookMarks.RemoveBookmarkAsync(userId, postId);

                if (!result)
                    return NotFound(new { message = "Bookmark not found" });

                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("User {UserId} removed bookmark for post {PostId}", userId, postId);

                return Ok(new { message = "Bookmark removed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing bookmark for post {PostId}", postId);
                return StatusCode(500, new { message = "An error occurred while removing bookmark" });
            }
        }

        [HttpGet("IsBookmarked")]
        public async Task<IActionResult> CheckBookmark([FromQuery]Guid postId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated" });

                var isBookmarked = await _unitOfWork.BookMarks.IsBookmarkedAsync(userId, postId);

                return Ok(new
                {
                    postId = postId,
                    isBookmarked = isBookmarked
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking bookmark status for post {PostId}", postId);
                return StatusCode(500, new { message = "An error occurred" });
            }
        }
    }
}