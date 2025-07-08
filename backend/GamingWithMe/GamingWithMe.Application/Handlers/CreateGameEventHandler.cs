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
    public class CreateGameEventHandler : IRequestHandler<CreateGameEventCommand, GameEventDto>
    {
        private readonly IAsyncRepository<GameEvent> _eventRepository;
        private readonly IAsyncRepository<Game> _gameRepository;
        private readonly IMapper _mapper;

        public CreateGameEventHandler(
            IAsyncRepository<GameEvent> eventRepository,
            IAsyncRepository<Game> gameRepository,
            IMapper mapper)
        {
            _eventRepository = eventRepository;
            _gameRepository = gameRepository;
            _mapper = mapper;
        }

        public async Task<GameEventDto> Handle(CreateGameEventCommand request, CancellationToken cancellationToken)
        {
            // Verify the game exists
            var game = await _gameRepository.GetByIdAsync(request.GameId, cancellationToken);
            if (game == null)
            {
                throw new ApplicationException($"Game with ID {request.GameId} not found");
            }

            // Create the event
            var gameEvent = new GameEvent(
                request.Title,
                request.StartDate,
                request.EndDate,
                request.PrizePool,
                request.NumberOfTeams,
                request.Location,
                request.GameId
            );

            // Save to repository
            await _eventRepository.AddAsync(gameEvent, cancellationToken);
            
            // Set the game for mapping
            gameEvent.Game = game;

            // Return DTO
            return _mapper.Map<GameEventDto>(gameEvent);
        }
    }
}