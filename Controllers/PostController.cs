using Waster.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Waster.DTOs;
namespace Waster.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PostController : ControllerBase
    {
        public AppDbContext context { get; set; }
        private readonly IBaseReporesitory<Post> _postRepo;
        private readonly ILogger<PostController> _logger;
        public PostController(AppDbContext Context, IBaseReporesitory<Post> postRep, ILogger<PostController> logger)
        {
            _postRepo = postRep;
            context = Context;
            _logger = logger;
        }


        //1
        [HttpGet("GetAllPosts")]//for user , auth required ** Reporesitory pattern approach
        public async Task<IActionResult> GetAllPosts()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == null)
            {
                return Unauthorized("User ID not found in token.");
            }
            var Posts = await _postRepo.SearchAndQuery(p => p.UserId == id && !p.IsDeleted,
                p => new Post
                {
                    Title = p.Title,
                    Description = p.Description,
                    Quantity = p.Quantity,
                    Unit = p.Unit,
                    Type = p.Type,
                    Status = p.Status,
                    IsValid = p.IsValid,
                    Created = p.Created,
                    Updated = p.Updated,
                    Notes = p.Notes,
                    PickupLocation = p.PickupLocation,
                    ExpiresOn = p.ExpiresOn,
                    ImageType = p.ImageType
                    ,Category = p.Category
                    //HasImage = p.ImageData != null,
                })
                .OrderByDescending(p => p.Created)
                .ToListAsync();


            if (!Posts.Any())
                return NotFound("No posts found for the specified user.");

            return Ok(Posts);
        }


        //2
        [HttpGet("SearchPosts")]//Search by title, type, status ** Reporesitory pattern approach
        public async Task<ActionResult<IEnumerable<PostDto>>> SearchPosts
            ([FromQuery] string title = null, [FromQuery] string type = null,
            [FromQuery] string status = null)
        {
            var posts = await _postRepo.SearchAndFilter(title, type, status,
                p => !p.IsDeleted && p.IsValid).OrderByDescending(p => p.Created)
                .ToListAsync();

            if (!posts.Any()) return NotFound("No posts was found");


            return Ok(posts);
        }


        //3
        [HttpGet("Get Post Image")]//get image by post id
        [AllowAnonymous]
        public async Task<IActionResult> GetPostImage([FromBody]Guid id)
        {
            if (!ModelState.IsValid) return BadRequest("the id is not valid");


            var post = await context.Posts.AsNoTracking()
                .Where(p => p.Id == id && !p.IsDeleted)
                .Select(p => new { p.ImageData, p.ImageType })
                .FirstOrDefaultAsync();

            if (post == null || post.ImageData == null)
                return NotFound("Couldnt reach the image the post might be deleted");

            return File(post.ImageData, post.ImageType ?? "image/jpeg");
        }

        //4
        [HttpGet("GetPost")]  // Name = "GetPost" for CreatedAtRoute ** reporesitory pattern approach
        public async Task<IActionResult> GetPost([FromQuery]Guid id)
        {
            if (!ModelState.IsValid) return BadRequest("the id is not valid");
            var post = await _postRepo.GetByIdAsync(id);  

            if (post == null || post.IsDeleted)
            {
                return NotFound(new { message = $"Post with ID {id} not found" });
            }

            return Ok(post);

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

            var post = new Post
            {

                // System-generated
                Id = Guid.NewGuid(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Status = "Available",
                IsValid = true,
                IsDeleted = false,
                Category = model.Category,

                // From authenticated user (NOT from client)
                UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException("User must be authenticated"),

                // From DTO (client input)
                Title = model.Title,
                Description = model.Description,
                Quantity = model.Quantity,
                Unit = model.Unit,
                Type = model.Type,
                ImageData = model.ImageData,
                ImageType = model.ImageType,
                Notes = model.Notes,
                PickupLocation = model.PickupLocation,
                ExpiresOn = model.ExpiresOn



                //Created = DateTime.UtcNow,
                //Status = "Available",
                //IsValid = true,
                //IsDeleted = false,
            };
            await _postRepo.AddAsync(post);
            await context.SaveChangesAsync();  //  Controller decides when to commit

            return CreatedAtRoute(routeName: "GetPost", routeValues: new { id = post.Id }, value: post);

        }

        //6
        [HttpPut("Edit post ")]
        public async Task<IActionResult> UpdatePost([FromQuery] Guid id, [FromBody] UpdatePostDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                //Get existing post from database
                var existingPost = await _postRepo.GetByIdAsync(id);

                if (existingPost == null)
                    return NotFound(new { message = $"Post with ID {id} not found" });

                //Map DTO properties to entity (only update what's provided)
                if (dto.Title != null)
                    existingPost.Title = dto.Title;

                if (dto.Description != null)
                    existingPost.Description = dto.Description;

                if (dto.Quantity != null)
                    existingPost.Quantity = dto.Quantity;

                if (dto.Unit != null)
                    existingPost.Unit = dto.Unit;

                if (dto.Type != null)
                    existingPost.Type = dto.Type;

                if (dto.PickupLocation != null)
                    existingPost.PickupLocation = dto.PickupLocation;

                if (dto.ExpiresOn.HasValue)
                    existingPost.ExpiresOn = dto.ExpiresOn.Value;

                if (dto.ImageData != null)
                    existingPost.ImageData = dto.ImageData;

                if (dto.ImageType != null)
                    existingPost.ImageType = dto.ImageType;

                if (dto.Notes != null)
                    existingPost.Notes = dto.Notes;
                if (dto.Category != null)
                    existingPost.Category = dto.Category;




                //Call repository update (marks as modified)
                await _postRepo.UpdateAsync(existingPost);

                await context.SaveChangesAsync();

                return Ok(existingPost);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict updating post {PostId}", id);
                return Conflict(new { message = "Post was modified by another user" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating post {PostId}", id);
                return StatusCode(500, new { message = "Error updating post" });
            }
        }


        //7
        [HttpDelete("Delete Post")] // soft delete Based on repository pattern
        public async Task<IActionResult> DeletePost([FromBody]Guid id)
        {

            try
            {
                var success = await _postRepo.DeleteAsync(id);

                if (!success)
                {
                    return NotFound(new { message = $"Post with ID {id} not found" });
                }

                await context.SaveChangesAsync();

                return NoContent();  // ← 204 status code (RESTful standard)
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while deleting post {PostId}", id);
                return StatusCode(500, new { message = "Failed to delete post due to database error" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post {PostId}", id);
                return StatusCode(500, new { message = "An unexpected error occurred" });
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
