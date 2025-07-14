using GamingWithMe.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Interfaces
{
    public interface IMessageRepository : IAsyncRepository<Message>
    {
        Task<IReadOnlyList<Message>> GetConversationAsync(Guid user1Id, Guid user2Id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Message>> GetUserMessagesAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Guid>> GetChatPartnerIdsAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
