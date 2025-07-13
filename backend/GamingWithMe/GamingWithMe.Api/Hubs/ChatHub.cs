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

        private static readonly Dictionary<string, string> userConnectionMap = new Dictionary<string, string>();
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

            var receiver = await _repo.GetByIdAsync(receiverId);

            if(userConnectionMap.TryGetValue(receiver.UserId, out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveMessage", message);

            }

            //// Send to specific user if online
            //await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", result);

            // Also send back to sender to confirm
            await Clients.Caller.SendAsync("ReceiveMessage", result);
        }

        public override Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            userConnectionMap[userId] = Context.ConnectionId;
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.UserIdentifier;
            userConnectionMap.Remove(userId);
            return base.OnDisconnectedAsync(exception);
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
