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

        public BookMarksController(AppDbContext context,  IBaseReporesitory<Post> postRepo, BookMarkBL bookMarkBL)
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

            var posts = BookMarkBL.GetBookMark(userId);
            if (posts == null)
                return NotFound(new { Message = "No BookMarks found" });

            return Ok(posts);
        }

        
        [HttpPost(Name = "Add-Post-to-BookMark")]
        public async Task<ActionResult<BookMark>> AddPost([FromQuery]Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not found" });

            var bookMark = await BookMarkBL.AddPostToBookMark(userId, id);

            _context.BookMarks.Add(bookMark);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (BookMarkExists(bookMark.Id))
                {
                    return Conflict(new { Message = " BookMark couldnt be saved due an error"});
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetBookMark", new { id = bookMark.Id }, bookMark);
        }


        [HttpDelete]
        public async Task<IActionResult> DeleteBookMark([FromQuery]string Bookmarkid)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new {Message= "User not found" });


            var bookmark = await _context.BookMarks
                .Where(b => b.UserId.ToString() == userId && b.Id ==Bookmarkid)
                .Select(b => b.Id)
                .FirstOrDefaultAsync();
            if (bookmark == null)
                {
                return NotFound(new {Message= "No Bookmarks was found"});
            }

            var bookMark = await _context.BookMarks.FindAsync(bookmark);
            
            _context.BookMarks.Remove(bookMark);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool BookMarkExists(string id)
        {
            return _context.BookMarks.Any(e => e.Id == id);
        }
    }
}
