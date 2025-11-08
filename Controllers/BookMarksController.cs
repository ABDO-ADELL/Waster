using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Waster;
using Waster.Models.DbModels;
using Waster.Services;


namespace Waster.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BookMarksController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IBaseReporesitory<Post> _postRepo;
        private readonly BookMarkBL BookMarkBL;

        public BookMarksController(AppDbContext context, IBaseReporesitory<Post> postRepo, BookMarkBL bookMarkBL)
        {
            _context = context;
            _postRepo = postRepo;
            BookMarkBL = bookMarkBL;
        }




        [HttpGet(Name = "Get-BookMarks")]
        public async Task<IActionResult> GetBookMark()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not found" });

            var posts = await BookMarkBL.GetBookMark(userId);
            if (posts == null)
                return NotFound(new { Message = "No BookMarks found" });

            return Ok(posts);
        }


        [HttpPost(Name = "Add-Post-to-BookMark")]
        public async Task<ActionResult<BookMark>> AddPost([FromQuery] Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not found" });

            var bookMark = await BookMarkBL.AddPostToBookMark(userId, id);

            _context.BookMarks.Add(bookMark);

            await _context.SaveChangesAsync();



            return CreatedAtAction("GetBookMark", new { id = bookMark.Id }, bookMark);
        }


        [HttpDelete("{postId}")]
        public async Task<IActionResult> DeleteBookMark(Guid postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not found" });

            var bookMark = await _context.BookMarks
                .FirstOrDefaultAsync(b => b.UserId == userId && b.PostId == postId);

            if (bookMark == null)
                return NotFound(new { message = "Bookmark not found" });

            _context.BookMarks.Remove(bookMark);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    }
}
