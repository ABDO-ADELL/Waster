using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Waster.Services
{
    public interface IBaseReporesitory<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(Guid id);
        public IQueryable<TResult> SearchAndQuery<TResult>
        (Expression<Func<T, bool>> predicate,Expression<Func<T, TResult>> selector);
        public  Task<T> UpdateAsync(T entity);

        Task<IEnumerable<T>> Search(Expression<Func<T, bool>> predicate);
        public IQueryable<T> SearchAndFilter(string? title, string? type, string? status,
            Expression<Func<T, bool>> selector);

        Task<T> AddAsync(T entity);
     //   Task<T> UpdateAsync(T entity);
        Task<bool> DeleteAsync(Guid id);
        Task<(object items, int totalCount)> GetFeedAsync(string userId, int pageSize, string? category, bool excludeOwn);
    }
}
