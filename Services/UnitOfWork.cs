using Waster.Models;

namespace Waster.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UnitOfWork> _logger;
        private readonly IServiceProvider _serviceProvider;

        private IBaseReporesitory<Post>? _posts;
        private IBookMarkRepository? _bookMarks;
        private IBrowseRepository? _browse;

        public UnitOfWork(
            AppDbContext context,
            ILogger<UnitOfWork> logger,
            IServiceProvider serviceProvider)
        {
            _context = context;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public IBaseReporesitory<Post> Posts
        {
            get
            {
                if (_posts == null)
                {
                    _posts = new BaseReporesitory<Post>(_context);
                }
                return _posts;
            }
        }

        public IBookMarkRepository BookMarks
        {
            get
            {
                if (_bookMarks == null)
                {
                    var bookmarkLogger = _serviceProvider.GetRequiredService<ILogger<BookMarkRepository>>();
                    _bookMarks = new BookMarkRepository(_context, bookmarkLogger);
                }
                return _bookMarks;
            }
        }

        public IBrowseRepository Browse
        {
            get
            {
                if (_browse == null)
                {
                    var browseLogger = _serviceProvider.GetRequiredService<ILogger<BrowseRepository>>();
                    _browse = new BrowseRepository(_context, browseLogger);
                }
                return _browse;
            }
        }

        public async Task<int> CompleteAsync()
        {
            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes to database");
                throw;
            }
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}