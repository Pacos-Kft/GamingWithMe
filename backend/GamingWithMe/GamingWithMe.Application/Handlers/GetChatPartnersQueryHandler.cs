using AutoMapper;
using GamingWithMe.Application.Dtos;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Application.Queries;
using GamingWithMe.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Handlers
{
    public class GetChatPartnersQueryHandler : IRequestHandler<GetChatPartnersQuery, List<ProfileDto>>
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IAsyncRepository<User> _userRepository;
        private readonly IMapper _mapper;

        public GetChatPartnersQueryHandler(IMessageRepository messageRepository, IAsyncRepository<User> userRepository, IMapper mapper)
        {
            _messageRepository = messageRepository;
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<List<ProfileDto>> Handle(GetChatPartnersQuery request, CancellationToken cancellationToken)
        {
            var partnerIds = await _messageRepository.GetChatPartnerIdsAsync(request.UserId, cancellationToken);

            if (partnerIds == null || !partnerIds.Any())
            {
                return new List<ProfileDto>();
            }

            var partners = await _userRepository.ListAsync(cancellationToken);
            var filteredPartners = partners.Where(u => partnerIds.Contains(u.Id)).ToList();

            return _mapper.Map<List<ProfileDto>>(filteredPartners);
        }
    }
}