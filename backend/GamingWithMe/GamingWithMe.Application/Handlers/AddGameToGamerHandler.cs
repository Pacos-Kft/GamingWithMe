﻿using GamingWithMe.Application.Commands;
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
    public class AddGameToGamerHandler : IRequestHandler<AddGameToGamerCommand, bool>
    {
        private readonly IAsyncRepository<Gamer> _playerRepository;
        private readonly IAsyncRepository<Game> _gameRepository;
        private readonly IGamerReadRepository esportPlayerReadRepository;

        public AddGameToGamerHandler(IAsyncRepository<Gamer> playerRepository, IAsyncRepository<Game> gameRepository, IGamerReadRepository esportPlayerReadRepository)
        {
            _playerRepository = playerRepository;
            _gameRepository = gameRepository;
            this.esportPlayerReadRepository = esportPlayerReadRepository;
        }

        public async Task<bool> Handle(AddGameToGamerCommand request, CancellationToken cancellationToken)
        {
            var player = await esportPlayerReadRepository.GetByIdWithGamesAsync(request.userId, cancellationToken);

            if (player is not Gamer esportPlayer)
                throw new InvalidOperationException("Player not found or not an Esport player.");

            var game = await _gameRepository.GetByIdAsync(request.gameId, cancellationToken);
            if(game == null) throw new InvalidOperationException("Game not found.");

            var alreadyHasGame = esportPlayer.Games.Any(x=> x.GameId == game.Id);

            if (!alreadyHasGame)
            {
                esportPlayer.Games.Add(new GamerGame { GameId = game.Id, PlayerId = esportPlayer.Id});
            }

            await _playerRepository.Update(esportPlayer);

            return true;
        }
    }
}
