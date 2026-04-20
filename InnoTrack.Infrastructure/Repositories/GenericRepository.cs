using InnoTrack.Domain.Interfaces;
using InnoTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;

        public GenericRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<T?> GetByIdAsync(int id) => await _context.Set<T>().FindAsync(id);
        public async Task<T?> FindAsync(Expression<Func<T, bool>> predicate) => await _context.Set<T>().FirstOrDefaultAsync(predicate);

        public IQueryable<T> Query() => _context.Set<T>();
        public IQueryable<T> QueryAsNoTracking() => _context.Set<T>().AsNoTracking();
        public async Task<IReadOnlyList<T>> GetAllAsync(
                    Expression<Func<T, bool>>? filter = null,
                    Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
                    int pageIndex = 0,
                    int pageSize = 20)
        {
            IQueryable<T> query = _context.Set<T>().AsNoTracking();
            
            if(filter != null)
                query = query.Where(filter);

            if (orderBy != null)
                query = orderBy(query);

            return await query
                .Skip(pageSize * pageIndex)
                .Take(pageSize)
                .ToListAsync();
        }     
        public async Task AddAsync(T entity) => await _context.Set<T>().AddAsync(entity);
        public void Update(T entity) => _context.Set<T>().Update(entity);
        public void Delete(T entity) => _context.Set<T>().Remove(entity);
    }
}
