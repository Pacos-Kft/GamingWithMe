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
        private readonly IAsyncRepository<UserAvailability> _availabilityRepository;

        public GetBillingHistoryQueryHandler(IAsyncRepository<User> userRepository, IAsyncRepository<Booking> bookingRepository, IAsyncRepository<UserAvailability> availabilityRepository)
        {
            _userRepository = userRepository;
            _bookingRepository = bookingRepository;
            _availabilityRepository = availabilityRepository;
        }

        public async Task<List<BillingRecordDto>> Handle(GetBillingHistoryQuery request, CancellationToken cancellationToken)
        {
            var user = (await _userRepository.ListAsync(cancellationToken)).FirstOrDefault(u => u.UserId == request.UserId);
            if (user == null) return new List<BillingRecordDto>();

            var bookings = await _bookingRepository.ListAsync(cancellationToken, b => b.Provider, b => b.Customer);
            var userAvailabilities = await _availabilityRepository.ListAsync(cancellationToken);
            var userAvailabilityDict = userAvailabilities.ToDictionary(ua => ua.Id);

            var billingHistory = new List<BillingRecordDto>();

            // Bookings where the user was the customer (Paid)
            var paidBookings = bookings.Where(b => b.CustomerId == user.Id && b.StartTime <= DateTime.UtcNow);
            foreach (var booking in paidBookings)
            {
                if (userAvailabilityDict.TryGetValue(booking.Id, out var availability))
                {
                    billingHistory.Add(new BillingRecordDto(
                        booking.Id,
                        booking.StartTime,
                        availability.Price,
                        "Paid",
                        booking.Provider.Username,
                        booking.Provider.AvatarUrl
                    ));
                }
            }

            // Bookings where the user was the provider (Received)
            var receivedBookings = bookings.Where(b => b.ProviderId == user.Id && b.StartTime <= DateTime.UtcNow);
            foreach (var booking in receivedBookings)
            {
                if (userAvailabilityDict.TryGetValue(booking.Id, out var availability))
                {
                    billingHistory.Add(new BillingRecordDto(
                        booking.Id,
                        booking.StartTime,
                        availability.Price,
                        "Received",
                        booking.Customer.Username,
                        booking.Customer.AvatarUrl
                    ));
                }
            }

            return billingHistory.OrderByDescending(b => b.TransactionDate).ToList();
        }
    }
}