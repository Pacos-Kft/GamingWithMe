using GamingWithMe.Application.Interfaces;
using GamingWithMe.Domain.Entities;
using GamingWithMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Infrastructure.Repositories
{
    public class MessageRepository : EfRepository<Message>, IMessageRepository
    {
        private readonly ApplicationDbContext _ctx;

        public MessageRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            _ctx = dbContext;
        }

        public async Task<IReadOnlyList<Message>> GetConversationAsync(Guid user1Id, Guid user2Id, CancellationToken cancellationToken = default)
        {
            return await _ctx.Set<Message>()
                .Where(m => (m.SenderId == user1Id && m.ReceiverId == user2Id) ||
                            (m.SenderId == user2Id && m.ReceiverId == user1Id))
                .OrderBy(m => m.SentAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Message>> GetUserMessagesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _ctx.Set<Message>()
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync(cancellationToken);
        }
    }
}
