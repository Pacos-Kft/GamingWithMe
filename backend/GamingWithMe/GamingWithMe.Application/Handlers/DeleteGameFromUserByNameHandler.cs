using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Domain.Entities;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Handlers
{
    public class DeleteGameFromUserByNameHandler : IRequestHandler<DeleteGameFromUserByNameCommand, bool>
    {
        private readonly IAsyncRepository<User> _userRepo;
        private readonly IAsyncRepository<Game> _gameRepo;

        public DeleteGameFromUserByNameHandler(
            IAsyncRepository<User> userRepo,
            IAsyncRepository<Game> gameRepo)
        {
            _userRepo = userRepo;
            _gameRepo = gameRepo;
        }

        public async Task<bool> Handle(DeleteGameFromUserByNameCommand request, CancellationToken cancellationToken)
        {
            var user = (await _userRepo.ListAsync(cancellationToken)).FirstOrDefault(x => x.UserId == request.UserId);

            if (user is null)
                throw new InvalidOperationException("User not found");

            var game = (await _gameRepo.ListAsync(cancellationToken)).FirstOrDefault(x => x.Name.ToLower() == request.GameName.ToLower());
            if (game == null)
                throw new InvalidOperationException("Game not found.");

            var entry = user.Games.FirstOrDefault(x => x.GameId == game.Id);

            if (entry != null)
            {
                user.Games.Remove(entry);
                await _userRepo.Update(user);
                return true;
            }

            return false;
        }
    }
}