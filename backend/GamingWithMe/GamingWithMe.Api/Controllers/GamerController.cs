using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.Intrinsics.X86;
using System;
using System.Security.Claims;
using static System.Reflection.Metadata.BlobBuilder;
using GamingWithMe.Application.Dtos;

namespace GamingWithMe.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Esport")]
    public class GamerController : ControllerBase
    {


        private readonly IMediator _mediator;

        public GamerController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetProfile([FromQuery] string username)
        {
            var profile = await _mediator.Send(new GetGamerProfileQuery(username));

            return profile == null ? NotFound() : Ok(profile);
        }


        [HttpPost("languages")]
        public async Task<IActionResult> AddLanguage([FromBody] Guid languageId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null) { 
                return NotFound();
            }

            var added = await _mediator.Send(new AddLanguageToGamerCommand(userId, languageId));

            return Ok(added);
        }

        [HttpDelete("languages")]
        public async Task<IActionResult> DeleteLanguage([FromBody] string language)
        {
            var userId = GetUserId();

            var removed = await _mediator.Send(new DeleteLanguageFromGamerCommand(userId, language));

            return Ok(removed);
        }

        [HttpPost("games")]
        public async Task<IActionResult> AddGame([FromBody] Guid gameId)
        {
            var userId = GetUserId();

            var added = await _mediator.Send(new AddGameToGamerCommand(userId, gameId));

            return Ok(added);
        }

        [HttpDelete("games")]
        public async Task<IActionResult> DeleteGame([FromBody] Guid gameId)
        {
            var userId = GetUserId();

            var removed = await _mediator.Send(new DeleteGameFromGamerCommand(userId, gameId));

            return Ok(removed);
        }

        [HttpPut("status/active")]
        public async Task<IActionResult> SetIsActive()
        {
            var userId = GetUserId();
            
            var activity = await _mediator.Send(new SetGamerActivityCommand(userId));

            return Ok(activity);
        }

        [HttpPut("availability")]
        public async Task<IActionResult> SetAvailableHours([FromBody] WeeklyHoursDto dto)
        {
            var userId = GetUserId();

            var activity = await _mediator.Send(new SetAvailableHoursCommand(userId, dto));

            return Ok(activity);
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
