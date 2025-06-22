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
    public class GetEsportPlayerProfileHandler : IRequestHandler<GetEsportPlayerProfileQuery, EsportPlayerDto?>
    {

        private readonly IEsportPlayerReadRepository _repo;
        public GetEsportPlayerProfileHandler(IEsportPlayerReadRepository repo)
            => _repo = repo;

        public async Task<EsportPlayerDto?> Handle(GetEsportPlayerProfileQuery request, CancellationToken cancellationToken)
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
