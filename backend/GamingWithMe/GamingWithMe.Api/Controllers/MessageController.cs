using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Dtos;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Application.Queries;
using GamingWithMe.Domain.Entities;
using GamingWithMe.Infrastructure.Data;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GamingWithMe.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IAsyncRepository<User> _repo;
        private readonly ApplicationDbContext _context;

        public MessageController(IMediator mediator, IAsyncRepository<User> repo, ApplicationDbContext context)
        {
            _mediator = mediator;
            _repo = repo;
            _context = context;
        }

        [HttpGet("{recipientId}")]
        public async Task<IActionResult> GetConversationHistory(Guid recipientId)
        {
            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sender = (await _repo.ListAsync()).FirstOrDefault(x => x.UserId == senderId);


            var messages = await _context.Messages
                .Where(m => (m.SenderId == sender.Id && m.ReceiverId == recipientId) ||
                             (m.SenderId == recipientId && m.ReceiverId == sender.Id))
                .OrderBy(m => m.SentAt).ToListAsync();

            return Ok(messages);
        }

        [HttpGet("conversation/{otherUserId}")]
        public async Task<ActionResult<List<MessageDto>>> GetConversation(Guid otherUserId)
        {
            var currentUserId = (User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = (await _repo.ListAsync()).FirstOrDefault(x=> x.UserId == currentUserId);

            if (user == null) {
                return BadRequest("User not found");
            }

            var query = new GetChatQuery(user.Id, otherUserId);
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<MessageDto>> SendMessage([FromBody] SendMessageDto messageDto)
        {
            var currentUserId = (User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = (await _repo.ListAsync()).FirstOrDefault(x => x.UserId == currentUserId);

            if (user == null)
            {
                return BadRequest("User not found");
            }
            var command = new SendMessageCommand(user.Id, messageDto.ReceiverId, messageDto.Content);
            var result = await _mediator.Send(command);
            return Ok(result);
        }
    }

    public class SendMessageDto
    {
        public Guid ReceiverId { get; set; }
        public string Content { get; set; }
    }
}
