using GamingWithMe.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Interfaces
{
    public interface IEsportPlayerReadRepository
    {
        Task<EsportPlayerDto?> GetProfileByUsernameAsync(
            string username,
            CancellationToken ct = default);
    }
}
