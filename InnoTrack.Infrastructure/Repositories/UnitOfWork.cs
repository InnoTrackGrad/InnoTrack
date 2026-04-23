using InnoTrack.Domain.Interfaces;
using InnoTrack.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace InnoTrack.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private readonly IServiceProvider _serviceProvider;
        public UnitOfWork(ApplicationDbContext context, IServiceProvider serviceProvider)
        {
            _context = context;
            _serviceProvider = serviceProvider;
        }

        public IGenericRepository<T> Repository<T>() where T : class => 
            _serviceProvider.GetRequiredService<IGenericRepository<T>>();

        public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();

        public void Dispose() => _context.Dispose();
    }
}
