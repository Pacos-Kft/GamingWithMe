using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace GamingWithMe.Api.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IMediator _mediator;
        private readonly IAsyncRepository<User> _repo;


        public ChatHub(IMediator mediator, IAsyncRepository<User> repo)
        {
            _mediator = mediator;
            _repo = repo;
        }

        public async Task SendMessage(Guid receiverId, string message)
        {
            var userId = (Context.UserIdentifier);
            var sender = (await _repo.ListAsync()).FirstOrDefault(x=> x.UserId == userId);
            if (sender == null) {
                throw new Exception("User not found");
            }
            

            var command = new SendMessageCommand(sender.Id, receiverId, message);
            var result = await _mediator.Send(command);

            // Send to specific user if online
            await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", result);

            // Also send back to sender to confirm
            await Clients.Caller.SendAsync("MessageSent", result);
        }

        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }
    }
}
