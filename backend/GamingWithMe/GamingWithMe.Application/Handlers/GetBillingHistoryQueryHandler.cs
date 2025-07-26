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
    public class GetBillingHistoryQueryHandler : IRequestHandler<GetBillingHistoryQuery, List<BillingRecordDto>>
    {
        private readonly IAsyncRepository<User> _userRepository;
        private readonly IAsyncRepository<Booking> _bookingRepository;
        private readonly IAsyncRepository<ServiceOrder> _serviceOrderRepository;

        public GetBillingHistoryQueryHandler(
            IAsyncRepository<User> userRepository, 
            IAsyncRepository<Booking> bookingRepository,
            IAsyncRepository<ServiceOrder> serviceOrderRepository)
        {
            _userRepository = userRepository;
            _bookingRepository = bookingRepository;
            _serviceOrderRepository = serviceOrderRepository;
        }

        public async Task<List<BillingRecordDto>> Handle(GetBillingHistoryQuery request, CancellationToken cancellationToken)
        {
            var user = (await _userRepository.ListAsync(cancellationToken)).FirstOrDefault(u => u.UserId == request.UserId);
            if (user == null) return new List<BillingRecordDto>();

            var billingHistory = new List<BillingRecordDto>();

            // Get past bookings where this user was involved
            var bookings = await _bookingRepository.ListAsync(cancellationToken, b => b.Provider, b => b.Customer);
            var pastBookings = bookings.Where(b => b.StartTime <= DateTime.UtcNow).ToList();

            // Bookings where user was the customer (outgoing - paid out)
            var paidBookings = pastBookings.Where(b => b.CustomerId == user.Id);
            foreach (var booking in paidBookings)
            {
                billingHistory.Add(new BillingRecordDto(
                    booking.Id,
                    booking.StartTime,
                    booking.Amount, // Use the actual amount stored in booking
                    "Paid",
                    booking.Provider.Username,
                    booking.Provider.AvatarUrl
                ));
            }

            // Bookings where user was the provider (incoming - received payment)
            var receivedBookings = pastBookings.Where(b => b.ProviderId == user.Id);
            foreach (var booking in receivedBookings)
            {
                billingHistory.Add(new BillingRecordDto(
                    booking.Id,
                    booking.StartTime,
                    booking.Amount, // Use the actual amount stored in booking
                    "Received",
                    booking.Customer.Username,
                    booking.Customer.AvatarUrl
                ));
            }

            // Get completed or cancelled service orders
            var serviceOrders = await _serviceOrderRepository.ListAsync(cancellationToken, 
                so => so.Service, so => so.Customer, so => so.Provider);
            var completedOrders = serviceOrders.Where(so => 
                so.Status == OrderStatus.Completed || 
                so.Status == OrderStatus.Cancelled).ToList();

            // Service orders where user was the customer (outgoing - paid out)
            var paidOrders = completedOrders.Where(so => so.CustomerId == user.Id);
            foreach (var order in paidOrders)
            {
                billingHistory.Add(new BillingRecordDto(
                    order.Id,
                    order.OrderDate,
                    order.Service.Price, // Use the service price
                    "Paid",
                    order.Provider.Username,
                    order.Provider.AvatarUrl
                ));
            }

            // Service orders where user was the provider (incoming - received payment)
            var receivedOrders = completedOrders.Where(so => so.ProviderId == user.Id);
            foreach (var order in receivedOrders)
            {
                billingHistory.Add(new BillingRecordDto(
                    order.Id,
                    order.OrderDate,
                    order.Service.Price, // Use the service price
                    "Received",
                    order.Customer.Username,
                    order.Customer.AvatarUrl
                ));
            }

            return billingHistory.OrderByDescending(b => b.TransactionDate).ToList();
        }
    }
}