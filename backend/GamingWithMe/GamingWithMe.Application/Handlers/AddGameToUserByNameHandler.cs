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
    public class AddGameToUserByNameHandler : IRequestHandler<AddGameToUserByNameCommand, bool>
    {
        private readonly IAsyncRepository<User> _userRepo;
        private readonly IAsyncRepository<Game> _gameRepo;

        public AddGameToUserByNameHandler(
            IAsyncRepository<User> userRepo,
            IAsyncRepository<Game> gameRepo)
        {
            _userRepo = userRepo;
            _gameRepo = gameRepo;
        }

        public async Task<bool> Handle(AddGameToUserByNameCommand request, CancellationToken cancellationToken)
        {
            var user = (await _userRepo.ListAsync(cancellationToken)).FirstOrDefault(x => x.UserId == request.UserId);

            if (user is null)
                throw new InvalidOperationException("User not found");

            var game = (await _gameRepo.ListAsync(cancellationToken)).FirstOrDefault(x => x.Name.ToLower() == request.GameName.ToLower());
            if (game == null)
                throw new InvalidOperationException("Game not found.");

            var alreadyHasGame = user.Games.Any(x => x.GameId == game.Id);

            if (!alreadyHasGame)
            {
                user.Games.Add(new UserGame
                {
                    GameId = game.Id,
                    PlayerId = user.Id
                });
                
                await _userRepo.Update(user);
                return true;
            }

            return false; // Game already exists for user
        }
    }
}