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
    [Authorize]
    public class UserController : ControllerBase
    {


        private readonly IMediator _mediator;

        public UserController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ProfileDto>> GetProfile([FromQuery] string username)
        {
            var profile = await _mediator.Send(new GetUserProfileQuery(username));

            return profile == null ? NotFound() : Ok(profile);
        }


        [HttpPost("languages")]
        public async Task<IActionResult> AddLanguage([FromBody] Guid languageId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null) { 
                return NotFound();
            }

            var added = await _mediator.Send(new AddLanguageToUserCommand(userId, languageId));

            return Ok(added);
        }

        [HttpDelete("languages")]
        public async Task<IActionResult> DeleteLanguage([FromBody] string language)
        {
            var userId = GetUserId();

            var removed = await _mediator.Send(new DeleteLanguageFromUserCommand(userId, language));

            return Ok(removed);
        }

        [HttpPost("games")]
        public async Task<IActionResult> AddGame([FromBody] Guid gameId)
        {
            var userId = GetUserId();

            var added = await _mediator.Send(new AddGameToUserCommand(userId, gameId));

            return Ok(added);
        }

        [HttpDelete("games")]
        public async Task<IActionResult> DeleteGame([FromBody] Guid gameId)
        {
            var userId = GetUserId();

            var removed = await _mediator.Send(new DeleteGameFromUserCommand(userId, gameId));

            return Ok(removed);
        }

        [HttpPut("status/active")]
        public async Task<ActionResult<bool>> SetIsActive()
        {
            var userId = GetUserId();
            
            var activity = await _mediator.Send(new SetUserActivityCommand(userId));

            return Ok(activity);
        }

        [HttpPut("availability")]
        public async Task<IActionResult> SetAvailableHours([FromBody] WeeklyHoursDto dto)
        {
            var userId = GetUserId();

            var activity = await _mediator.Send(new SetAvailableHoursCommand(userId, dto));

            return Ok(activity);
        }

        [HttpPost("tags")]
        public async Task<IActionResult> AddTag([FromBody] Guid tagId)
        {
            var userId = GetUserId();
            var added = await _mediator.Send(new AddUserTagCommand(userId, tagId));
            return added ? Ok(true) : BadRequest("Failed to add tag");
        }

        [HttpDelete("tags/{tagId}")]
        public async Task<IActionResult> RemoveTag(Guid tagId)
        {
            var userId = GetUserId();
            var removed = await _mediator.Send(new RemoveUserTagCommand(userId, tagId));
            return removed ? Ok(true) : BadRequest("Failed to remove tag");
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
