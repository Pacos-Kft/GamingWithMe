using GamingWithMe.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GamingWithMe.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Esport")]
    public class EsportPlayerController : ControllerBase
    {
        //Get profile
        //Add language to player
        //delete langugae from player
        //add game to player
        //delete game from player

        private readonly IMediator _mediator;

        public EsportPlayerController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        public async Task<IActionResult> GetProfile([FromQuery] string username)
        {
            var profile = await _mediator.Send(new GetEsportPlayerProfileQuery(username));

            return profile == null ? NotFound() : Ok(profile);
        }




    }
}
