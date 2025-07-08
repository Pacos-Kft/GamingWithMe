using GamingWithMe.Application.Dtos;
using MediatR;
using System;

namespace GamingWithMe.Application.Commands
{
    public record CreateGameEasterEggCommand(
        string Description,
        string ImageUrl,
        Guid GameId
    ) : IRequest<GameEasterEggDto>;
}