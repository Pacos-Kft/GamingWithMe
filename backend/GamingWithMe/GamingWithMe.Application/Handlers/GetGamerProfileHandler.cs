using GamingWithMe.Application.Dtos;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Application.Queries;
using GamingWithMe.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Handlers
{
    public class GetGamerProfileHandler : IRequestHandler<GetGamerProfileQuery, GamerDto?>
    {

        private readonly IGamerReadRepository _repo;
        public GetGamerProfileHandler(IGamerReadRepository repo)
            => _repo = repo;

        public async Task<GamerDto?> Handle(GetGamerProfileQuery request, CancellationToken cancellationToken)
        {
            var dto = await _repo.GetProfileByUsernameAsync(request.username, cancellationToken);

            if (dto == null)
            {
                return null;
            }

            return dto;
        }
    }
}
