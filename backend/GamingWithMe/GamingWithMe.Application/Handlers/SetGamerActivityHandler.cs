using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Handlers
{
    public class SetGamerActivityHandler : IRequestHandler<SetGamerActivityCommand, bool>
    {
        private readonly IAsyncRepository<Gamer> _repo;

        public SetGamerActivityHandler(IAsyncRepository<Gamer> repo)
        {
            _repo = repo;
        }
        public async Task<bool> Handle(SetGamerActivityCommand request, CancellationToken cancellationToken)
        {
            var user = (await _repo.ListAsync(cancellationToken)).FirstOrDefault(x=> x.UserId == request.userId);

            if (user == null) {
                throw new InvalidOperationException("User not found");
            }

            user.IsActive = !user.IsActive;

            await _repo.Update(user);

            return user.IsActive;
        }
    }
}
