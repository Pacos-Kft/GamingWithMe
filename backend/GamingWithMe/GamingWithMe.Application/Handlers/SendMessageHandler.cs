using AutoMapper;
using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Dtos;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Handlers
{
    public class SendMessageHandler : IRequestHandler<SendMessageCommand, MessageDto>
    {
        private readonly IAsyncRepository<Message> _messageRepository;
        private readonly IMapper _mapper;

        public SendMessageHandler(IAsyncRepository<Message> messageRepository, IMapper mapper)
        {
            _messageRepository = messageRepository;
            _mapper = mapper;
        }

        public async Task<MessageDto> Handle(SendMessageCommand request, CancellationToken cancellationToken)
        {
            var message = new Message
            {
                SenderId = request.SenderId,
                ReceiverId = request.ReceiverId,
                Content = request.Content,
                SentAt = DateTime.UtcNow
            };

            await _messageRepository.AddAsync(message, cancellationToken);
            return _mapper.Map<MessageDto>(message);
        }
    }
}
