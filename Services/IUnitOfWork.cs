using Waster.Models;

namespace Waster.Services
{
    public interface IUnitOfWork : IDisposable
    {
        IBaseReporesitory<Post> Posts { get; }
        IBookMarkRepository BookMarks { get; }
        IBrowseRepository Browse { get; }
        public Task<int> CompleteAsync();


    }
}
