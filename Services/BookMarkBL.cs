using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Waster.Models.DbModels;
using Waster.Services;


namespace Waster.Services
{
    public class BookMarkBL
    {
        private readonly AppDbContext _context;
        private readonly IBaseReporesitory<Post> _postRepo;

        public BookMarkBL(AppDbContext context, IBaseReporesitory<Post> postRepo)
        {
            _context = context;
            _postRepo = postRepo;
        }


        public async Task<List<Post>> GetBookMark(string userId)
        {

            var user = await _context.Users.FindAsync(userId);

            var postIds = await _context.BookMarks.AsNoTracking()
                .Where(b => b.UserId.ToString() == user.Id)
                .Select(b => b.PostId)
                .ToListAsync();

            if (!postIds.Any())
                return null;

            var posts = new List<Post>();
            foreach (var postId in postIds)
            {
                var post = await _postRepo.GetByIdAsync(postId);
                if (post != null)
                {
                    posts.Add(post);
                }
            }

            return posts;
        }

        public async Task<BookMark> AddPostToBookMark(string userId, Guid postId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("User not found");
            var post = await _postRepo.GetByIdAsync(postId);
            if (post == null)
                throw new Exception("Post not found");

            var existingBookMark = await _context.BookMarks
            .FirstOrDefaultAsync(b => b.UserId.ToString() == userId && b.PostId == postId);
            if (existingBookMark != null)
            {
                throw new Exception( "Post is already bookmarked");
            }


            var bookMark = new BookMark
            {
                Id = Guid.NewGuid().ToString(),
                UserId = Guid.Parse(userId),
                PostId = postId
            };
            return bookMark;
        }




    }
}
