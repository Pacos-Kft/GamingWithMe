using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Handlers
{
    public class AddGameToPlayerHandler : IRequestHandler<AddGameToPlayerCommand, bool>
    {
        private readonly IAsyncRepository<EsportPlayer> _playerRepository;
        private readonly IAsyncRepository<Game> _gameRepository;
        private readonly IEsportPlayerReadRepository esportPlayerReadRepository;

        public AddGameToPlayerHandler(IAsyncRepository<EsportPlayer> playerRepository, IAsyncRepository<Game> gameRepository, IEsportPlayerReadRepository esportPlayerReadRepository)
        {
            _playerRepository = playerRepository;
            _gameRepository = gameRepository;
            this.esportPlayerReadRepository = esportPlayerReadRepository;
        }

        public async Task<bool> Handle(AddGameToPlayerCommand request, CancellationToken cancellationToken)
        {
            var player = await esportPlayerReadRepository.GetByIdWithGamesAsync(request.userId, cancellationToken);

            if (player is not EsportPlayer esportPlayer)
                throw new InvalidOperationException("Player not found or not an Esport player.");

            var game = await _gameRepository.GetByIdAsync(request.gameId, cancellationToken);
            if(game == null) throw new InvalidOperationException("Game not found.");

            var alreadyHasGame = esportPlayer.Games.Any(x=> x.GameId == game.Id);

            if (!alreadyHasGame)
            {
                esportPlayer.Games.Add(new EsportGame { GameId = game.Id, PlayerId = esportPlayer.Id});
            }

            await _playerRepository.Update(esportPlayer);

            return true;
        }
    }
}
