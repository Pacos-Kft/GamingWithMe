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
    public sealed class EsportPlayerReadRepository
    : IEsportPlayerReadRepository
    {
        private readonly ApplicationDbContext _ctx;
        private readonly IMapper _mapper;

        public EsportPlayerReadRepository(ApplicationDbContext ctx, IMapper mapper)
        {
            _ctx = ctx;
            _mapper = mapper;
        }

        //TODO consider this
        //public async Task<EsportPlayer?> GetByIdAsync(string id, CancellationToken ct = default, params Expression<Func<EsportPlayer, object>>[] includes)
        //{
        //    IQueryable<EsportPlayer> query = _ctx.EsportPlayers;

        //    foreach (var include in includes)
        //        query = query.Include(include);

        //    return await query.FirstOrDefaultAsync(p => p.UserId == id, ct);
        //}

        public async Task<EsportPlayer?> GetByIdWithLanguagesAsync(string id, CancellationToken ct = default)
        {
            return await _ctx.EsportPlayers
            .Include(p => p.Languages)
            .FirstOrDefaultAsync(p => p.UserId == id);
        }

        public async Task<EsportPlayer?> GetByIdWithGamesAsync(string id, CancellationToken ct = default)
        {
            return await _ctx.EsportPlayers
            .Include(p => p.Games)
            .FirstOrDefaultAsync(p => p.UserId == id);
        }

        public async Task<EsportPlayerDto?> GetProfileByUsernameAsync(
            string username, CancellationToken ct = default)
        {
            return await _ctx.EsportPlayers
                .AsNoTracking()
                .Where(p => p.Username == username)
                .Select(p => new EsportPlayerDto(
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
