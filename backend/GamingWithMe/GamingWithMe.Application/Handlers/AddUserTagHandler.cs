using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Handlers
{
    public class AddUserTagHandler : IRequestHandler<AddUserTagCommand, bool>
    {
        private readonly IAsyncRepository<User> _userRepository;
        private readonly IAsyncRepository<Tag> _tagRepository;
        private readonly IAsyncRepository<UserTag> _userTagRepository;

        public AddUserTagHandler(
            IAsyncRepository<User> userRepository,
            IAsyncRepository<Tag> tagRepository,
            IAsyncRepository<UserTag> userTagRepository)
        {
            _userRepository = userRepository;
            _tagRepository = tagRepository;
            _userTagRepository = userTagRepository;
        }

        public async Task<bool> Handle(AddUserTagCommand request, CancellationToken cancellationToken)
        {
            // Check if user exists


            var user = (await _userRepository.ListAsync(cancellationToken)).FirstOrDefault(x=> x.UserId == request.UserId);
            if (user == null)
                return false;

            // Check if tag exists
            var tag = await _tagRepository.GetByIdAsync(request.TagId, cancellationToken);
            if (tag == null)
                return false;

            // Check if the tag is already assigned to the user
            var existingItems = await _userTagRepository.ListAsync(cancellationToken);
            var alreadyExists = existingItems.Any(ut => ut.UserId == user.Id && ut.TagId == request.TagId);
            
            if (alreadyExists)
                return true; // Already has the tag, consider it successful

            // Create and save the new user-tag relationship
            var userTag = new UserTag
            {
                UserId = user.Id,
                TagId = request.TagId
            };

            await _userTagRepository.AddAsync(userTag, cancellationToken);
            return true;
        }
    }
}