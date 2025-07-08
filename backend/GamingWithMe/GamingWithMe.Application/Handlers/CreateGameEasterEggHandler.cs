using AutoMapper;
using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Dtos;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Handlers
{
    public class CreateGameEasterEggHandler : IRequestHandler<CreateGameEasterEggCommand, GameEasterEggDto>
    {
        private readonly IAsyncRepository<GameEasterEgg> _easterEggRepository;
        private readonly IAsyncRepository<Game> _gameRepository;
        private readonly IMapper _mapper;

        public CreateGameEasterEggHandler(
            IAsyncRepository<GameEasterEgg> easterEggRepository,
            IAsyncRepository<Game> gameRepository,
            IMapper mapper)
        {
            _easterEggRepository = easterEggRepository;
            _gameRepository = gameRepository;
            _mapper = mapper;
        }

        public async Task<GameEasterEggDto> Handle(CreateGameEasterEggCommand request, CancellationToken cancellationToken)
        {
            // Verify the game exists
            var game = await _gameRepository.GetByIdAsync(request.GameId, cancellationToken);
            if (game == null)
            {
                throw new ApplicationException($"Game with ID {request.GameId} not found");
            }

            // Create the easter egg
            var easterEgg = new GameEasterEgg(
                request.Description,
                request.ImageUrl,
                request.GameId
            );

            // Save to repository
            await _easterEggRepository.AddAsync(easterEgg, cancellationToken);
            
            // Set the game for mapping
            easterEgg.Game = game;

            // Return DTO
            return _mapper.Map<GameEasterEggDto>(easterEgg);
        }
    }
}