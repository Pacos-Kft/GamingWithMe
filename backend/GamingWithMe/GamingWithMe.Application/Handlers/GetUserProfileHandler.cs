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
    public class GetUserProfileHandler : IRequestHandler<GetUserProfileQuery, ProfileDto?>
    {
        private readonly IAsyncRepository<User> _repo;
        public GetUserProfileHandler(IAsyncRepository<User> repo)
            => _repo = repo;

        public async Task<ProfileDto?> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
        {
            var user = (await _repo.ListAsync(cancellationToken)).FirstOrDefault(x=> x.Username == request.username);

            if (user == null)
            {
                return null;
            }

            //TODO use mapper
            //TODO include the correct fields

            //return new ProfileDto(
            //    username: user.Username,
            //    avatarurl: user.AvatarUrl,
            //    bio: user.Bio,
            //    games: user.Games?.Select(g => g.Game.Name).ToList() ?? new List<string>(),
            //    languages: user.Languages?.Select(l => l.Language.Name).ToList() ?? new List<string>(),
            //    joined: user.CreatedAt
            //);

            return null;
        }
    }
}
