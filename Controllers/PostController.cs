using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading;
using Waster.DTOs;
using Waster.Helpers;
using Waster.Services;
namespace Waster.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PostController : ControllerBase
    {
        private readonly AppDbContext context;
        private readonly IBaseReporesitory<Post> _postRepo;
        private readonly ILogger<PostController> _logger;
        private readonly IFileStorageService _fileStorage;
        //private readonly BookMarkBL _bookmarkbl;

        public PostController(
        AppDbContext _context, IBaseReporesitory<Post> postRepo, ILogger<PostController> logger, IFileStorageService fileStorage)
        {
            _postRepo = postRepo;
            context = _context;
            _logger = logger;
            _fileStorage = fileStorage;
            //_bookmarkbl = bookMarkBL;
        }

        [HttpPost("Create-Post")]// Create new post ** Reporesitory pattern approach
        public async Task<IActionResult> CreatePost(PostDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // Business validation
            if (model.ExpiresOn <= DateTime.UtcNow)
            {
                return BadRequest(new { message = "Expiry date must be in the future" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized("User must be authenticated to create a post");
            }

            string? imageUrl = null;
            if (!string.IsNullOrEmpty(model.ImageData))
            {
                try
                {
                    _logger.LogInformation("Processing image data");

                    var base64 = model.ImageData.Trim();

                    // Remove data URI prefix if present
                    if (base64.Contains(","))
                    {
                        var parts = base64.Split(',');
                        base64 = parts[parts.Length - 1];
                    }

                    // Remove any whitespace, newlines, or invalid characters
                    base64 = base64.Replace(" ", "")
                                   .Replace("\n", "")
                                   .Replace("\r", "")
                                   .Replace("\t", "");

                    // Validate base64 string length (must be multiple of 4)
                    int mod4 = base64.Length % 4;
                    if (mod4 > 0)
                    {
                        base64 += new string('=', 4 - mod4); // Add padding
                    }

                    _logger.LogInformation("Base64 length after cleanup: {Length}", base64.Length);

                    // Decode base64 → byte[]
                    byte[] imageBytes;
                    try
                    {
                        imageBytes = Convert.FromBase64String(base64);
                    }
                    catch (FormatException ex)
                    {
                        _logger.LogError(ex, "Invalid base64 format. First 50 chars: {Preview}",
                            base64.Length > 50 ? base64.Substring(0, 50) : base64);
                        return BadRequest(new
                        {
                            message = "Invalid image format. Please ensure the image is properly encoded in base64.",
                            detail = "Base64 string contains invalid characters or incorrect format"
                        });
                    }

                    _logger.LogInformation("Image decoded successfully ({Size} bytes)", imageBytes.Length);

                    // Validate image size (optional: max 5MB)
                    if (imageBytes.Length > 5 * 1024 * 1024)
                    {
                        return BadRequest(new { message = "Image size cannot exceed 5MB" });
                    }

                    // Validate minimum size (at least 100 bytes for a valid image)
                    if (imageBytes.Length < 100)
                    {
                        return BadRequest(new { message = "Image data is too small to be valid" });
                    }

                    // Save using your file storage service
                    imageUrl = await _fileStorage.SaveImageAsync(
                        imageBytes,
                        model.ImageType ?? "image/jpeg");

                    _logger.LogInformation("Image saved: {Url}", imageUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save image. ImageData length: {Length}",
                        model.ImageData?.Length ?? 0);
                    return BadRequest(new
                    {
                        message = "Failed to process image data.",
                        detail = ex.Message
                    });
                }
            }


            var post = new Post
            {
                Id = Guid.NewGuid(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Status = "Available",
                IsValid = true,
                IsDeleted = false,
                UserId = userId,
                Title = model.Title,
                Description = model.Description,
                Quantity = model.Quantity,
                Unit = model.Unit,
                Category = model.Category,
                ImageUrl = imageUrl, // "/uploads/images/abc123.jpg"
                 PickupLocation = model.PickupLocation,
                ExpiresOn = model.ExpiresOn
             };
            if (string.IsNullOrEmpty(model.PickupLocation))
            {
                var user = await context.Users.FindAsync(userId);
                user.Address = model.PickupLocation;
            }


            await _postRepo.AddAsync(post);
            await context.SaveChangesAsync();  //  Controller decides when to commit

            var responseDto = post.ToResponseDto();
            return Ok();

            //return Ok(new { responseDto });
            //return CreatedAtRoute(routeName: "GetPost", routeValues: new { id = post.Id }, value: post);

        }


        [HttpPut("Edit-post")]
        public async Task<IActionResult> UpdatePost([FromQuery] Guid id, [FromBody] UpdatePostDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid update data");

            var existingPost = await _postRepo.GetByIdAsync(id);
            if (existingPost == null)
                return NotFound("Post Not found");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (existingPost.UserId != userId)
                return Forbid("You are not authorized to update this post");

            // Handle image replacement
            if (!string.IsNullOrEmpty(dto.ImageData))
            {

                // Delete old image if it exists
                if (!string.IsNullOrEmpty(existingPost.ImageUrl))
                {
                    try
                    {
                        _fileStorage.DeleteImageAsync(existingPost.ImageUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete old image");
                    }
                }

                string? imageUrl = null;
                {
                    try
                    {
                        _logger.LogInformation("Processing image data");

                        var base64 = dto.ImageData.Trim();

                        // Remove data URI prefix if present
                        if (base64.Contains(","))
                        {
                            var parts = base64.Split(',');
                            base64 = parts[parts.Length - 1];
                        }

                        // Remove any whitespace, newlines, or invalid characters
                        base64 = base64.Replace(" ", "")
                                       .Replace("\n", "")
                                       .Replace("\r", "")
                                       .Replace("\t", "");

                        // Validate base64 string length (must be multiple of 4)
                        int mod4 = base64.Length % 4;
                        if (mod4 > 0)
                        {
                            base64 += new string('=', 4 - mod4); // Add padding
                        }

                        _logger.LogInformation("Base64 length after cleanup: {Length}", base64.Length);

                        // Decode base64 → byte[]
                        byte[] imageBytes;
                        try
                        {
                            imageBytes = Convert.FromBase64String(base64);
                        }
                        catch (FormatException ex)
                        {
                            _logger.LogError(ex, "Invalid base64 format. First 50 chars: {Preview}",
                                base64.Length > 50 ? base64.Substring(0, 50) : base64);
                            return BadRequest(new
                            {
                                message = "Invalid image format. Please ensure the image is properly encoded in base64.",
                                detail = "Base64 string contains invalid characters or incorrect format"
                            });
                        }

                        _logger.LogInformation("Image decoded successfully ({Size} bytes)", imageBytes.Length);

                        // Validate image size (optional: max 5MB)
                        if (imageBytes.Length > 5 * 1024 * 1024)
                        {
                            return BadRequest(new { message = "Image size cannot exceed 5MB" });
                        }

                        // Validate minimum size (at least 100 bytes for a valid image)
                        if (imageBytes.Length < 100)
                        {
                            return BadRequest(new { message = "Image data is too small to be valid" });
                        }

                        // Save using your file storage service
                        imageUrl = await _fileStorage.SaveImageAsync(
                            imageBytes,
                            dto.ImageType ?? "image/jpeg");


                        _logger.LogInformation("Image saved: {Url}", imageUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save image. ImageData length: {Length}",
                            dto.ImageData?.Length ?? 0);
                        return BadRequest(new
                        {
                            message = "Failed to process image data.",
                            detail = ex.Message
                        });
                    }
                    existingPost.ImageUrl = imageUrl;
                }


            }

                // Update other fields
                if (dto.Title != null) existingPost.Title = dto.Title;
            if (dto.Description != null) existingPost.Description = dto.Description;
            if (dto.Quantity != null) existingPost.Quantity = dto.Quantity;
            if (dto.Unit != null) existingPost.Unit = dto.Unit;
            if (dto.PickupLocation != null) existingPost.PickupLocation = dto.PickupLocation;
            if (dto.ExpiresOn.HasValue) existingPost.ExpiresOn = dto.ExpiresOn.Value;
            if (dto.Category != null) existingPost.Category = dto.Category;

            await _postRepo.UpdateAsync(existingPost);
            await context.SaveChangesAsync();

            return Ok(existingPost.ToResponseDto());
        }

        
        [HttpDelete("Delete-Post")] // soft delete Based on repository pattern
        public async Task<IActionResult> DeletePost([FromQuery]Guid id)
        {

            {
                var post = await context.Posts.FindAsync(id);
                if (post == null)
                    return NotFound("Post not found ");

                // Delete image from file system
                if (!string.IsNullOrEmpty(post.ImageUrl))
                {
                    try
                    {
                        _logger.LogInformation("Deleting image: {Url}", post.ImageUrl);
                         _fileStorage.DeleteImageAsync(post.ImageUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete image, continuing with post deletion");
                    }
                }

                var success = await _postRepo.DeleteAsync(id);
                if (!success)
                    return NotFound("Post not found");

                await context.SaveChangesAsync();
                return NoContent();
            }
        }
    }
}
