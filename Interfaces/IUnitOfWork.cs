using Waster.Models;

namespace Waster.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IBaseReporesitory<Post> Posts { get; }
        IBookMarkRepository BookMarks { get; }
        IBrowseRepository Browse { get; }
        public Task<int> CompleteAsync();


    }
}
