using GamingWithMe.Application.Commands;
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
    public class CreateServiceOrderHandler : IRequestHandler<CreateServiceOrderCommand, Guid>
    {
        private readonly IAsyncRepository<User> _userRepository;
        private readonly IAsyncRepository<FixedService> _serviceRepository;
        private readonly IAsyncRepository<ServiceOrder> _orderRepository;

        public CreateServiceOrderHandler(
            IAsyncRepository<User> userRepository,
            IAsyncRepository<FixedService> serviceRepository,
            IAsyncRepository<ServiceOrder> orderRepository)
        {
            _userRepository = userRepository;
            _serviceRepository = serviceRepository;
            _orderRepository = orderRepository;
        }

        public async Task<Guid> Handle(CreateServiceOrderCommand request, CancellationToken cancellationToken)
        {
            var customer = (await _userRepository.ListAsync(cancellationToken))
                .FirstOrDefault(u => u.UserId == request.CustomerId);

            if (customer == null)
                throw new InvalidOperationException("Customer not found");

            var service = await _serviceRepository.GetByIdAsync(request.OrderDto.ServiceId, cancellationToken, s => s.User);

            if (service == null)
                throw new InvalidOperationException("Service not found");

            if (service.Status != ServiceStatus.Active)
                throw new InvalidOperationException("Service is not available");

            var order = new ServiceOrder(
                service.Id,
                customer.Id,
                service.UserId,
                request.PaymentIntentId,
                service.GetDeadlineInDays(),
                request.OrderDto.CustomerNotes
            );

            await _orderRepository.AddAsync(order, cancellationToken);
            return order.Id;
        }
    }
}