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
    public class DeleteGameFromPlayerHandler : IRequestHandler<DeleteGameFromPlayerCommand, bool>
    {
        private readonly IAsyncRepository<EsportPlayer> _playerRepository;
        private readonly IAsyncRepository<Game> _gameRepository;
        private readonly IEsportPlayerReadRepository esportPlayerReadRepository;

        public DeleteGameFromPlayerHandler(IAsyncRepository<EsportPlayer> playerRepository, IAsyncRepository<Game> gameRepository, IEsportPlayerReadRepository esportPlayerReadRepository)
        {
            _playerRepository = playerRepository;
            _gameRepository = gameRepository;
            this.esportPlayerReadRepository = esportPlayerReadRepository;
        }

        public async Task<bool> Handle(DeleteGameFromPlayerCommand request, CancellationToken cancellationToken)
        {
            var player = await esportPlayerReadRepository.GetByIdWithGamesAsync(request.userId, cancellationToken);

            if (player is not EsportPlayer esportPlayer)
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
