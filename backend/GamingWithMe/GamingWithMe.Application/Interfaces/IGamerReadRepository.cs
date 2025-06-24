using GamingWithMe.Application.Dtos;
using GamingWithMe.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Interfaces
{
    public interface IGamerReadRepository
    {
        Task<Gamer?> GetByIdAsync(string id, CancellationToken ct = default, params Expression<Func<Gamer, object>>[] includes);

        Task<GamerDto?> GetProfileByUsernameAsync(
            string username,
            CancellationToken ct = default);

        Task<Gamer?> GetByIdWithLanguagesAsync(
            string id,
            CancellationToken ct = default);

        Task<Gamer?> GetByIdWithGamesAsync(
            string id,
            CancellationToken ct = default);
    }
}
