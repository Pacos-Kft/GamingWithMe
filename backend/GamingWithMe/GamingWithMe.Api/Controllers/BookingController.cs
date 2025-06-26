using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GamingWithMe.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BookingController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BookingController(IMediator mediator) => _mediator = mediator;

        [HttpPost("{userid}")]
        public async Task<ActionResult<BookingDetailsDto>> Create(Guid userid, [FromBody] BookingDetailsDto dto)
        {
            var userId = GetUserId();

            var booked = await _mediator.Send(new BookingCommand(userid, userId, dto));




            return Ok(dto);
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
