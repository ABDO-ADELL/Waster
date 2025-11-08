using Microsoft.AspNetCore.Authorization;
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
        AppDbContext _context,IBaseReporesitory<Post> postRepo,ILogger<PostController> logger,  IFileStorageService fileStorage) 
        {
            _postRepo = postRepo;
            context = _context;
            _logger = logger;
            _fileStorage = fileStorage;
            //_bookmarkbl = bookMarkBL;
        }

        [HttpGet("Get All user's Posts")]
        public async Task<IActionResult> GetAllPosts([FromQuery] PaginationParams paginationParams)
        {
            #region verify user id from token
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized("User ID not found in token.");
            #endregion
            var query = context.Posts
                .Where(p => p.UserId == userId && !p.IsDeleted)
                .OrderByDescending(p => p.Created);

            // pagination
            var pagedPosts = await query.ToPaginatedListAsync(
                paginationParams?.PageNumber?? 1,
                paginationParams?.PageSize?? 10);

            if (!pagedPosts.Items.Any())
                return NotFound("No posts found for the specified user.");

            var postDtos = pagedPosts.Items.Select(p => p.ToListItemDto()).ToList();

            return Ok(new
            {
                items = postDtos,
                pageNumber = pagedPosts.PageNumber,
                pageSize = pagedPosts.PageSize,
                totalCount = pagedPosts.TotalCount,
                totalPages = pagedPosts.TotalPages,
                hasPrevious = pagedPosts.HasPrevious,
                hasNext = pagedPosts.HasNext
            });
        }


        //2  
        [HttpGet("Search Posts")]
        public async Task<IActionResult> SearchPosts
        ([FromQuery] string? title = null,[FromQuery] string? type = null,[FromQuery] string? status = null,
         [FromQuery] PaginationParams paginationParams = null)
        {
            paginationParams ??= new PaginationParams(); // Default if not provided

            var query = _postRepo.SearchAndFilter(title, type, status, p => !p.IsDeleted && p.IsValid)
                .OrderByDescending(p => p.Created);

            var pagedPosts = await query.ToPaginatedListAsync(
                paginationParams.PageNumber,
                paginationParams.PageSize);

            if (!pagedPosts.Items.Any())
                return NotFound("No posts found");
            var postDtos = pagedPosts.Items.Select(p => p.ToListItemDto()).ToList();

            return Ok(new
            {
                items = postDtos,
                pageNumber = pagedPosts.PageNumber,
                pageSize = pagedPosts.PageSize,
                totalCount = pagedPosts.TotalCount,
                totalPages = pagedPosts.TotalPages,
                hasPrevious = pagedPosts.HasPrevious,
                hasNext = pagedPosts.HasNext
            });
        }


        //4
        [HttpGet("GetPost/{id}", Name = "GetPost")]
        public async Task<IActionResult> GetPost(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized("User ID not found in token.");

            var post = await _postRepo.GetByIdAsync(id);

            if (post == null || post.IsDeleted)
            {
                return NotFound(new { message = $"Post with ID {id} not found" });
            }

            // Check if bookmarked efficiently
            //bool isBookMarked = await context.BookMarks
            //    .AnyAsync(b => b.UserId == userId && b.PostId == id);

            bool isOwner = post.UserId == userId;

            return Ok(new
            {
                post = post.ToResponseDto(includeOwner: isOwner),
                //isBookMarked
            });
        }

        //5
        [HttpPost("CreatePost")]// Create new post ** Reporesitory pattern approach
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

            var user = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (user == null)
            {
                return Unauthorized("User must be authenticated to create a post");
            }

            string? imageUrl = null;
            if (model.ImageData != null && model.ImageData.Length > 0)
            {
                try
                {
                    _logger.LogInformation("Saving image to file system ({Size} bytes)", model.ImageData.Length);

                    // Save and get relative path
                    imageUrl = await _fileStorage.SaveImageAsync(
                        model.ImageData,
                        model.ImageType ?? "image/jpeg"
                    );

                    _logger.LogInformation("Image saved: {Url}", imageUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save image");
                    throw new Exception("Failed to save image. Please try again.");
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
                UserId = user,
                Title = model.Title,
                Description = model.Description,
                Quantity = model.Quantity,
                Unit = model.Unit,
                Type = model.Type,
                Category = model.Category,
                ImageUrl = imageUrl, // "/uploads/images/abc123.jpg"
                Notes = model.Notes,
                PickupLocation = model.PickupLocation,
                ExpiresOn = model.ExpiresOn
            };

            await _postRepo.AddAsync(post);
            await context.SaveChangesAsync();  //  Controller decides when to commit

            var responseDto = post.ToResponseDto();
            return CreatedAtRoute("GetPost", new { id = post.Id }, responseDto);


            //return CreatedAtRoute(routeName: "GetPost", routeValues: new { id = post.Id }, value: post);

        }

        //6
        [HttpPut("Edit post")]
        public async Task<IActionResult> UpdatePost([FromQuery] Guid id, [FromBody] UpdatePostDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid update data");

            var existingPost = await _postRepo.GetByIdAsync(id);
            if (existingPost == null)
                return NotFound("Post Not found");

            // Handle image replacement
            if (dto.ImageData != null && dto.ImageData.Length > 0)
            {
                try
                {
                    // Delete old image
                    if (!string.IsNullOrEmpty(existingPost.ImageUrl))
                    {
                        _logger.LogInformation("Deleting old image: {Url}", existingPost.ImageUrl);
                         _fileStorage.DeleteImageAsync(existingPost.ImageUrl);
                    }

                    // Save new image
                    _logger.LogInformation("Saving new image");
                    existingPost.ImageUrl = await _fileStorage.SaveImageAsync(
                        dto.ImageData,
                        dto.ImageType ?? "image/jpeg"
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update image");
                    throw new Exception("Failed to update image. Please try again.");
                }
            }

            // Update other fields
            if (dto.Title != null) existingPost.Title = dto.Title;
            if (dto.Description != null) existingPost.Description = dto.Description;
            if (dto.Quantity != null) existingPost.Quantity = dto.Quantity;
            if (dto.Unit != null) existingPost.Unit = dto.Unit;
            if (dto.Type != null) existingPost.Type = dto.Type;
            if (dto.PickupLocation != null) existingPost.PickupLocation = dto.PickupLocation;
            if (dto.ExpiresOn.HasValue) existingPost.ExpiresOn = dto.ExpiresOn.Value;
            if (dto.Notes != null) existingPost.Notes = dto.Notes;
            if (dto.Category != null) existingPost.Category = dto.Category;

            await _postRepo.UpdateAsync(existingPost);
            await context.SaveChangesAsync();

            return Ok(existingPost.ToResponseDto());


        }


        //7
        [HttpDelete("Delete Post")] // soft delete Based on repository pattern
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



//[HttpPut("UpdatePost")]
//public async Task<IActionResult> UpdatePost([FromBody] PostDto model)
//{

//    var post = await context.Posts.FirstOrDefaultAsync(p => p.Id == model.Id);
//    if (post == null)
//    {
//        return NotFound("Post not found.");
//    }

//    post.Title = model.Title;
//    post.Description = model.Description;
//    post.PickupLocation = model.PickupLocation;
//    post.Quantity = model.Quantity;
//    post.ExpiresOn = model.ExpiresOn;
//    post.UserId = model.UserId;
//    post.Updated = DateTime.UtcNow;
//    post.Status = model.Status;
//    post.ImageData = model.ImageData;
//    post.ImageType = model.ImageType;
//    post.Type = model.Type;
//    post.Unit = model.Unit;
//    post.IsValid = model.IsValid;
//    post.IsDeleted = model.IsDeleted;
//    post.Notes = model.Notes;
//    // context.Posts.Update(post); no need after fetching it from db
//    await context.SaveChangesAsync();
//    return Ok($"Post updated ");
//}
