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
    public class DeleteGameFromGamerHandler : IRequestHandler<DeleteGameFromGamerCommand, bool>
    {
        private readonly IAsyncRepository<Gamer> _playerRepository;
        private readonly IAsyncRepository<Game> _gameRepository;
        private readonly IGamerReadRepository esportPlayerReadRepository;

        public DeleteGameFromGamerHandler(IAsyncRepository<Gamer> playerRepository, IAsyncRepository<Game> gameRepository, IGamerReadRepository esportPlayerReadRepository)
        {
            _playerRepository = playerRepository;
            _gameRepository = gameRepository;
            this.esportPlayerReadRepository = esportPlayerReadRepository;
        }

        public async Task<bool> Handle(DeleteGameFromGamerCommand request, CancellationToken cancellationToken)
        {
            var player = await esportPlayerReadRepository.GetByIdWithGamesAsync(request.userId, cancellationToken);

            if (player is not Gamer esportPlayer)
                throw new InvalidOperationException("Player not found or not an Esport player.");

            var game = await _gameRepository.GetByIdAsync(request.gameId, cancellationToken);
            if (game == null) throw new InvalidOperationException("Game not found.");

            var entry = esportPlayer.Games.FirstOrDefault(x=> x.GameId == game.Id);

            if (entry != null)
            {
                player.Games.Remove(entry);
                await _playerRepository.Update(player);
                return true;
            }

            return false;
        }
    }
}
