using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Waster.Controllers;
using Waster.DTOs;
using Waster.Helpers;
using Waster.Interfaces;

namespace Waster.Services
{
    public class PostService : IPostService
    {
        private readonly AppDbContext _context;
        private readonly IBaseReporesitory<Post> _postRepo;
        private readonly ILogger<PostService> _logger;
        private readonly IFileStorageService _fileStorage;
        private readonly IHttpContextAccessor _Accessor;
        public PostService(AppDbContext context, IHttpContextAccessor accessor, ILogger<PostService> logger, IBaseReporesitory<Post> baseReporesitory
            , IFileStorageService fileStorageService)
        {
            _context = context;
            _Accessor = accessor;
            _logger = logger;
            _postRepo = baseReporesitory;
            _fileStorage = fileStorageService;
        }
        public async Task<ResponseDto<object>> CreatePost(PostDto model)
        {
            // Business validation
            if (model.ExpiresOn <= DateTime.UtcNow)
            {
                return new ResponseDto<object>
                {
                    Success = false,
                    Message = "Expiration date must be in the future"
                };
            }

            var userId = _Accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return new ResponseDto<object> { Message = "User must be authenticated to create a post", Success = false };
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
                        return new ResponseDto<object>
                        {
                            Success = false,
                            Message = "Invalid image format. Please ensure the image is properly encoded in base64 , Base64 string contains invalid characters or incorrect format"
                        };
                    }

                    _logger.LogInformation("Image decoded successfully ({Size} bytes)", imageBytes.Length);

                    // Validate image size (optional: max 5MB)
                    if (imageBytes.Length > 5 * 1024 * 1024)
                    {
                        return new ResponseDto<object> { Success = false, Message = "Image size cannot exceed 5MB" };
                    }

                    // Validate minimum size (at least 100 bytes for a valid image)
                    if (imageBytes.Length < 100)
                    {
                        return new ResponseDto<object> { Success = false, Message = "Image data is too small to be valid" };
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
                    return new ResponseDto<object>
                    {
                        Success = false,
                        Message = "Failed to process image data. " + ex.Message
                    };
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
                var user = await _context.Users.FindAsync(userId);
                user.Address = model.PickupLocation;
            }


            await _postRepo.AddAsync(post);
            var dashboard = await _context.dashboardStatus.FirstAsync(u => u.UserId == userId);
            dashboard.AvailablePosts += 1;
            await _context.SaveChangesAsync();


            return new ResponseDto<object>
            {
                Success = true,
                Message = "Post created"
            };
            //return Ok(new { responseDto });
            //return CreatedAtRoute(routeName: "GetPost", routeValues: new { id = post.Id }, value: post);
        }
        public async Task<ResponseDto<object>> UpdatePost( Guid id, UpdatePostDto dto)
       {
            var existingPost = await _postRepo.GetByIdAsync(id);
            if (existingPost == null)
                return new ResponseDto<object> { Success=false,Message = "Post Not found" };

            var userId = _Accessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (existingPost.UserId != userId)
                return new ResponseDto<object> { Success = false, Message = "You are not authorized to update this post" };

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
                            return new ResponseDto<object>
                            {
                                Success = false,
                                Message = "Invalid image format. Please ensure the image is properly encoded in base64. Base64 string contains invalid characters or incorrect format"
                            };
                        }

                        _logger.LogInformation("Image decoded successfully ({Size} bytes)", imageBytes.Length);

                        // Validate image size (optional: max 5MB)
                        if (imageBytes.Length > 5 * 1024 * 1024)
                        {
                            return new ResponseDto<object> { Success=false ,Message = "Image size cannot exceed 5MB" };
                        }

                        // Validate minimum size (at least 100 bytes for a valid image)
                        if (imageBytes.Length < 100)
                        {
                            return new ResponseDto<object> { Success=false ,Message = "Image data is too small to be valid" };
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
                        return new ResponseDto<object>
                        {
                            Success = false,
                            Message = "Failed to process image data." + ex.Message
                        };
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
            await _context.SaveChangesAsync();

            return new ResponseDto<object> { Message="Updated",Success= true };
        }
        public async Task<ResponseDto<object>> DeletePost(Guid id)
        {
            var userId = _Accessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var post = await _context.Posts.FindAsync(id);

            if (post.UserId != userId)
                return new ResponseDto<object> { Success = false, Message = "You are not authorized to delete this post" };

                if (post == null)
                    return new ResponseDto<object> { Message = "Post not found " , Success =false};

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
                    return new ResponseDto<object> { Success=false,Message="Post not found" };
            var dashboard = await _context.dashboardStatus.FirstOrDefaultAsync(u => u.UserId == userId);
            dashboard.AvailablePosts -= 1;
            await _context.SaveChangesAsync();


            return new ResponseDto<object> { Message="Post deleted successfully",Success=true};
        }

    }
}
