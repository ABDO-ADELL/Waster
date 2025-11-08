using Microsoft.EntityFrameworkCore;
using Waster.Models.DbModels;


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


            var posts = await _context.BookMarks
                 .AsNoTracking()
                .Where(b => b.UserId == userId)
                 .Include(b => b.Post)
                .ThenInclude(p => p.AppUser)
                .Select(b => b.Post)
                .Where(p => !p.IsDeleted && p.IsValid)
                .ToListAsync();

            return posts;


        }

        public async Task<BookMark> AddPostToBookMark(string userId, Guid postId)
        {
            var post = await _postRepo.GetByIdAsync(postId);
            if (post == null || post.IsDeleted || !post.IsValid)
                throw new KeyNotFoundException("Post not found");

            var existingBookMark = await _context.BookMarks
                .FirstOrDefaultAsync(b => b.UserId == userId && b.PostId == postId);

            if (existingBookMark != null)
                throw new InvalidOperationException("Post is already bookmarked");

            var bookMark = new BookMark
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PostId = postId
            };
            return bookMark;
        }




    }
}
