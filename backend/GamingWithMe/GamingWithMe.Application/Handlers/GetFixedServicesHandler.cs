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

        public GetFixedServicesHandler(IAsyncRepository<FixedService> serviceRepository)
        {
            _serviceRepository = serviceRepository;
        }

        public async Task<List<FixedServiceDto>> Handle(GetFixedServicesQuery request, CancellationToken cancellationToken)
        {
            var services = await _serviceRepository.ListAsync(cancellationToken, s => s.User, s => s.User.Tags);
            
            var filteredServices = services.AsQueryable();

            if (!string.IsNullOrEmpty(request.UserId))
            {
                filteredServices = filteredServices.Where(s => s.User.UserId == request.UserId);
            }

            if (!string.IsNullOrEmpty(request.Category))
            {
                filteredServices = filteredServices.Where(s => 
                    s.User.Tags.Any(ut => ut.Tag.Name.Equals(request.Category, StringComparison.OrdinalIgnoreCase)));
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