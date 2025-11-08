using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Waster.Helpers;
using Waster.Services;

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

        [HttpGet]
        public async Task<IActionResult> GetBookmarks()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated" });

                var posts = await _unitOfWork.BookMarks.GetUserBookmarksAsync(userId);

                if (!posts.Any())
                    return Ok(new { message = "No bookmarks found", items = new List<object>() });

                var postDtos = posts.Select(p => p.ToListItemDto()).ToList();

                return Ok(new
                {
                    items = postDtos,
                    count = postDtos.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bookmarks");
                return StatusCode(500, new { message = "An error occurred while retrieving bookmarks" });
            }
        }

        [HttpPost("{postId}")]
        public async Task<IActionResult> AddBookmark(Guid postId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated" });

                var bookmark = await _unitOfWork.BookMarks.AddBookmarkAsync(userId, postId);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("User {UserId} bookmarked post {PostId}", userId, postId);

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
                _logger.LogError(ex, "Error adding bookmark for post {PostId}", postId);
                return StatusCode(500, new { message = "An error occurred while adding bookmark" });
            }
        }

        [HttpDelete("{postId}")]
        public async Task<IActionResult> RemoveBookmark(Guid postId)
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

        [HttpGet("check/{postId}")]
        public async Task<IActionResult> CheckBookmark(Guid postId)
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