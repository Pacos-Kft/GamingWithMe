using GamingWithMe.Application.Dtos;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Application.Queries;
using GamingWithMe.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Handlers
{
    public class GetFixedServicesHandler : IRequestHandler<GetFixedServicesQuery, List<FixedServiceDto>>
    {
        private readonly IAsyncRepository<FixedService> _serviceRepository;
        private readonly IAsyncRepository<User> _userRepository;
        private readonly IAsyncRepository<Tag> _tagRepository;

        public GetFixedServicesHandler(
            IAsyncRepository<FixedService> serviceRepository,
            IAsyncRepository<User> userRepository,
            IAsyncRepository<Tag> tagRepository)
        {
            _serviceRepository = serviceRepository;
            _userRepository = userRepository;
            _tagRepository = tagRepository;
        }

        public async Task<List<FixedServiceDto>> Handle(GetFixedServicesQuery request, CancellationToken cancellationToken)
        {
            // Get services with just the User included
            var services = await _serviceRepository.ListAsync(cancellationToken, s => s.User);
            
            // Get all users with their tags
            var users = await _userRepository.ListAsync(cancellationToken, u => u.Tags);
            var allTags = await _tagRepository.ListAsync(cancellationToken);
            
            // Create dictionaries for efficient lookups
            var userDict = users.ToDictionary(u => u.Id);
            var tagDict = allTags.ToDictionary(t => t.Id);
            
            // Manually populate the Tag navigation properties
            foreach (var user in users)
            {
                foreach (var userTag in user.Tags)
                {
                    if (tagDict.TryGetValue(userTag.TagId, out var tag))
                    {
                        userTag.Tag = tag;
                    }
                }
            }
            
            // Update services with the populated user data
            foreach (var service in services)
            {
                if (userDict.TryGetValue(service.UserId, out var populatedUser))
                {
                    service.User = populatedUser;
                }
            }
            
            var filteredServices = services.AsQueryable();

            if (!string.IsNullOrEmpty(request.UserId))
            {
                filteredServices = filteredServices.Where(s => s.User.UserId == request.UserId);
            }

            if (!string.IsNullOrEmpty(request.Category))
            {
                filteredServices = filteredServices.Where(s => 
                    s.User.Tags.Any(ut => ut.Tag != null && ut.Tag.Name.Equals(request.Category, StringComparison.OrdinalIgnoreCase)));
            }

            if (request.IsCustomService.HasValue)
            {
                filteredServices = filteredServices.Where(s => s.IsCustomService == request.IsCustomService.Value);
            }

            return filteredServices
                .Where(s => s.Status == ServiceStatus.Active)
                .Select(s => new FixedServiceDto(
                    s.Id,
                    s.Title,
                    s.Description,
                    s.Price,
                    s.DeliveryDeadline,
                    s.Status,
                    s.User.Username,
                    s.User.AvatarUrl,
                    s.IsCustomService,
                    s.CreatedAt
                ))
                .OrderByDescending(s => s.CreatedAt)
                .ToList();
        }
    }
}