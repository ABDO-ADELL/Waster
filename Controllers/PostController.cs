using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;
using System.Threading;
using Waster.DTOs;
using Waster.Helpers;
using Waster.Interfaces;
namespace Waster.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PostController : ControllerBase
    {
        //private readonly AppDbContext _context;
        //private readonly IBaseReporesitory<Post> _postRepo;
        //private readonly ILogger<PostController> _logger;
        //private readonly IFileStorageService _fileStorage;
        //private readonly BookMarkBL _bookmarkbl;
        private readonly IPostService _postService;
        public PostController( IPostService postService  )
        {
            _postService = postService;
        }

        [HttpPost()]
        public async Task<IActionResult> Post(PostDto model)
        {
            var ResponseDto = await _postService.CreatePost(model);
            if (!ResponseDto.Success)
            {
                return BadRequest(ResponseDto.Message);
            }
            return Ok(ResponseDto.Message);
        }
        [HttpPut]
        public async Task<IActionResult> Post([FromQuery] Guid id, [FromBody] UpdatePostDto dto)
        {
            var responseDto = await _postService.UpdatePost(id, dto);
            if (!responseDto.Success)
            {
                return BadRequest(responseDto.Message);
            }
            return Ok(responseDto.Message);
        }
        
        [HttpDelete] 
        public async Task<IActionResult> Post([FromQuery]Guid id)
        {
            var responseDto = await _postService.DeletePost(id);
            if (!responseDto.Success)
            {
                return BadRequest(responseDto.Message);
            }
            return Ok(responseDto.Message);
        }
    }
}
