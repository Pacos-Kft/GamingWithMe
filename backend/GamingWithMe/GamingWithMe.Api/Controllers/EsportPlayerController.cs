using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GamingWithMe.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Esport")]
    public class EsportPlayerController : ControllerBase
    {
        //Get profile - done
        //Add language to player - done
        //delete langugae from player - done
        //add game to player - 
        //delete game from player - 

        private readonly IMediator _mediator;

        public EsportPlayerController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetProfile([FromQuery] string username)
        {
            var profile = await _mediator.Send(new GetEsportPlayerProfileQuery(username));

            return profile == null ? NotFound() : Ok(profile);
        }


        [HttpPost("languages")]
        public async Task<IActionResult> AddLanguage([FromBody] Guid languageId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null) { 
                return NotFound();
            }

            var added = await _mediator.Send(new AddLanguageToPlayerCommand(userId, languageId));

            return Ok(added);
        }

        [HttpDelete("languages")]
        public async Task<IActionResult> DeleteLanguage([FromBody] string language)
        {
            var userId = GetUserId();

            var removed = await _mediator.Send(new DeleteLanguageFromPlayerCommand(userId, language));

            return Ok(removed);
        }

        [HttpPost("games")]
        public async Task<IActionResult> AddGame([FromBody] Guid gameId)
        {
            var userId = GetUserId();

            var added = await _mediator.Send(new AddGameToPlayerCommand(userId, gameId));

            return Ok(added);
        }

        [HttpDelete("games")]
        public async Task<IActionResult> DeleteGame([FromBody] Guid gameId)
        {
            var userId = GetUserId();

            var removed = await _mediator.Send(new DeleteGameFromPlayerCommand(userId, gameId));

            return Ok(removed);
        }

        private string GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                throw new Exception("User not found");
            }

            return userId;
        }




    }
}
