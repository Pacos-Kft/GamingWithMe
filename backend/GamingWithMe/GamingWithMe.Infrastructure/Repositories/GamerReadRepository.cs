using AutoMapper;
using AutoMapper.QueryableExtensions;
using GamingWithMe.Application.Dtos;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Domain.Entities;
using GamingWithMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Infrastructure.Repositories
{
    public sealed class GamerReadRepository
    : IGamerReadRepository
    {
        private readonly ApplicationDbContext _ctx;
        private readonly IMapper _mapper;

        public GamerReadRepository(ApplicationDbContext ctx, IMapper mapper)
        {
            _ctx = ctx;
            _mapper = mapper;
        }

        //TODO consider this
        public async Task<Gamer?> GetByIdAsync(string id, CancellationToken ct = default, params Expression<Func<Gamer, object>>[] includes)
        {
            IQueryable<Gamer> query = _ctx.Gamers;

            foreach (var include in includes)
                query = query.Include(include);

            return await query.FirstOrDefaultAsync(p => p.UserId == id, ct);
        }

        public async Task<Gamer?> GetByIdWithLanguagesAsync(string id, CancellationToken ct = default)
        {
            return await _ctx.Gamers
            .Include(p => p.Languages)
            .FirstOrDefaultAsync(p => p.UserId == id);
        }

        public async Task<Gamer?> GetByIdWithGamesAsync(string id, CancellationToken ct = default)
        {
            return await _ctx.Gamers
            .Include(p => p.Games)
            .FirstOrDefaultAsync(p => p.UserId == id);
        }

        public async Task<GamerDto?> GetProfileByUsernameAsync(
            string username, CancellationToken ct = default)
        {
            return await _ctx.Gamers
                .AsNoTracking()
                .Where(p => p.Username == username)
                .Select(p => new GamerDto(
                    p.Username,
                    p.AvatarUrl,
                    p.Bio,
                    p.Games.Select(g => g.Game.Name).ToList(),
                    p.Languages.Select(l => l.Language.Name).ToList(),
                    p.Earnings,
                    p.CreatedAt))
                .FirstOrDefaultAsync();

        }


    }

}
