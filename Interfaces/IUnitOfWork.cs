using Waster.Models;

namespace Waster.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IBaseReporesitory<Post> Posts { get; }
        IBookMarkRepository BookMarks { get; }
        IBrowseService Browse { get; }
        public Task<int> CompleteAsync();


    }
}
