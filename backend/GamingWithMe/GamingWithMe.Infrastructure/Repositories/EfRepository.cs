using GamingWithMe.Application.Interfaces;
using GamingWithMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Infrastructure.Repositories
{
    internal sealed class EfRepository<T> : IAsyncRepository<T> where T : class
    {
        private readonly ApplicationDbContext _ctx;

        public EfRepository(ApplicationDbContext ctx) => _ctx = ctx;

        public async Task AddAsync(T entity, CancellationToken ct = default)
        {
            await _ctx.Set<T>().AddAsync(entity, ct);
            await _ctx.SaveChangesAsync();
        }

        public async void Delete(T entity)
        {
            _ctx.Set<T>().Remove(entity);
            await _ctx.SaveChangesAsync();

        }

        public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _ctx.Set<T>().FindAsync(id,ct);
        }

        public async Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default)
        {
            return await _ctx.Set<T>().AsNoTracking().ToListAsync(ct);
        }

        public async void Update(T entity)
        {
            _ctx.Update(entity);
            await _ctx.SaveChangesAsync();
        }
    }
}
