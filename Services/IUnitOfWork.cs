using Waster.Models;

namespace Waster.Services
{
    public interface IUnitOfWork : IDisposable
    {
        // IBaseReporesitory<Post> Posts { get; }
        // IBaseReporesitory<AppUser> Users { get; }
        IBaseReporesitory<Post> Posts { get; }
        public Task<int> CompleteAsync();


    }
}
