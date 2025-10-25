//using Waster.Models;

//namespace Waster.Services
//{
//    public class UnitOfWork : IUnitOfWork
//    {
//        private readonly AppDbContext _context;
//        public readonly IBaseReporesitory<Post> _posts;
//        public readonly IBaseReporesitory<AppUser> _users;
//        public UnitOfWork(IBaseReporesitory<Post> posts, IBaseReporesitory<AppUser> users, AppDbContext context)
//        {
//            _context = context;

//            _posts = new BaseReporesitory<Post>(_context) ;
//            _users = new BaseReporesitory<AppUser>(_context);

//        }

//        public async Task<int> CompleteAsync() => await _context.SavingChangesAsync();

//        public void Dispose()
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
