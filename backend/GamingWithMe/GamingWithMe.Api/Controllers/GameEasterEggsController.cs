using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Dtos;
using GamingWithMe.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GamingWithMe.Api.Controllers
{
    [Route("api/games/{gameId}/easter-eggs")]
    [ApiController]
    public class GameEasterEggsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public GameEasterEggsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<List<GameEasterEggDto>>> GetEasterEggs(Guid gameId)
        {
            var easterEggs = await _mediator.Send(new GetGameEasterEggsByGameIdQuery(gameId));
            return Ok(easterEggs);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<GameEasterEggDto>> CreateEasterEgg(Guid gameId, [FromBody] CreateGameEasterEggCommand command)
        {
            if (gameId != command.GameId)
            {
                return BadRequest("GameId in URL must match GameId in request body");
            }

            try
            {
                var result = await _mediator.Send(command);
                return CreatedAtAction(nameof(GetEasterEggs), new { gameId = gameId }, result);
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{easterEggId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteEasterEgg(Guid gameId, Guid easterEggId)
        {
            var result = await _mediator.Send(new DeleteGameEasterEggCommand(easterEggId));
            
            if (!result)
            {
                return NotFound();
            }
            
            return NoContent();
        }
    }
}