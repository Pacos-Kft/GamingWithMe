using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GamingWithMe.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LanguagesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public LanguagesController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        public async Task<IActionResult> List(CancellationToken ct)
            => Ok(await _mediator.Send(new GetAllLanguagesQuery(), ct));

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(string language, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateLanguageCommand(language), ct));
    }
}
