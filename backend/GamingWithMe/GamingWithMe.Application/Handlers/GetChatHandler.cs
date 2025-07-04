using AutoMapper;
using GamingWithMe.Application.Dtos;
using GamingWithMe.Application.Handlers;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Application.Queries;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Handlers
{
    public class GetChatHandler : IRequestHandler<GetChatQuery, List<MessageDto>>
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IMapper _mapper;

        public GetChatHandler(IMessageRepository messageRepository, IMapper mapper)
        {
            _messageRepository = messageRepository;
            _mapper = mapper;
        }

        public async Task<List<MessageDto>> Handle(GetChatQuery request, CancellationToken cancellationToken)
        {
            var messages = await _messageRepository.GetConversationAsync(request.User1Id, request.User2Id, cancellationToken);
            return _mapper.Map<List<MessageDto>>(messages);
        }
    }
}
