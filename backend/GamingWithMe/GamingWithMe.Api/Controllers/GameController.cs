using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Dtos;
using GamingWithMe.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GamingWithMe.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GameController : ControllerBase
    {
        private readonly IMediator _mediator;

        public GameController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{gameId}")]
        public async Task<ActionResult<GameDto>> GetById(Guid gameId)
        {
            var query = new GetGameByIdQuery(gameId);
            var result = await _mediator.Send(query);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<GameDto>>> GetAll()
        {
            var games = await _mediator.Send(new GetAllGamesQuery());
            return Ok(games);
        }

        [HttpPost]
        public async Task<IActionResult> CreateGame([FromBody] GameDto gameDto)
        {
            var command = new CreateGameCommand(gameDto);

            var gameId = await _mediator.Send(command);

            return CreatedAtAction(nameof(GetById), new { gameId }, gameDto);
        }
    }
}
