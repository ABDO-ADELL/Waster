using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Waster.Services
{
    public class BaseReporesitory<T> : IBaseReporesitory<T> where T : class
    {
        public readonly AppDbContext _context;
        public BaseReporesitory(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _context.Set<T>().ToListAsync();
        }
        public async Task<T?> GetByIdAsync(Guid id)
        {
            var query = _context.Set<T>().AsQueryable();

            query = query.Where(e => EF.Property<Guid>(e, "Id") == id);

            
            return await query.FirstOrDefaultAsync();

        }


        public async Task<T> AddAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
            return entity;

        }
        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null)
                return false;
            var isDeletedProperty = _context.Model.FindEntityType(typeof(T))?.FindProperty("IsDeleted");
            if (isDeletedProperty != null)
            {
                // Soft delete
                _context.Entry(entity).Property("IsDeleted").CurrentValue = true;
                _context.Entry(entity).State = EntityState.Modified;
            }
            else
            {
                // Hard delete
                _context.Remove(entity);
            }
            //await _context.SaveChangesAsync();
            return true;
        }

        public IQueryable<TResult> SearchAndQuery<TResult>
            (Expression<Func<T, bool>> predicate,
            Expression<Func<T, TResult>> selector)
        {
            return _context.Set<T>().Where(predicate).Select(selector);
        }


        public async Task<IEnumerable<T>> Search(Expression<Func<T, bool>> predicate)
        {
            return await _context.Set<T>().Where(predicate).ToListAsync();
        }


        public virtual async Task<T> UpdateAsync(T entity)
        {
            var entry = _context.Entry(entity);

            if (entry.State == EntityState.Detached)
            {
                // Not tracked - attach it and mark as modified
                _context.Attach(entity);
                entry.State = EntityState.Modified;
            }
            else if (entry.State == EntityState.Unchanged)
            {
                // Tracked but no changes detected - force modified
                entry.State = EntityState.Modified;
            }

            // Auto-update "Updated" timestamp if property exists
            var updatedProp = entry.Properties
                .FirstOrDefault(p => p.Metadata.Name == "Updated");

            if (updatedProp != null)
            {
                updatedProp.CurrentValue = DateTime.UtcNow;
            }

            return entity;
        }


        public IQueryable<T> SearchAndFilter(string? title, string? type, string? status,
            Expression<Func<T, bool>> Filter)
        {
            var query = _context.Set<T>().Where(Filter);
            if (!string.IsNullOrWhiteSpace(title) && typeof(T).GetProperty("Title") != null)
                query = query.Where(p => EF.Property<string>(p, "Title").ToLower().Contains(title.ToLower()));
            if (!string.IsNullOrWhiteSpace(type) && typeof(T).GetProperty("Type") != null)
                query = query.Where(p => EF.Property<string>(p, "Type").ToLower() == type.ToLower());
            if (!string.IsNullOrWhiteSpace(status) && typeof(T).GetProperty("Status") != null)
                query = query.Where(p => EF.Property<string>(p, "Status").ToLower() == status.ToLower());

            return query;
        }

     }
}
