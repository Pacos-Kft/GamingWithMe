using AutoMapper;
using AutoMapper.QueryableExtensions;
using GamingWithMe.Application.Dtos;
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
