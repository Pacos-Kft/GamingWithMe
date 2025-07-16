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
    public class GetUpcomingBookingsQueryHandler : IRequestHandler<GetUpcomingBookingsQuery, List<UpcomingBookingDto>>
    {
        private readonly IAsyncRepository<User> _userRepository;
        private readonly IAsyncRepository<Booking> _bookingRepository;

        public GetUpcomingBookingsQueryHandler(IAsyncRepository<User> userRepository, IAsyncRepository<Booking> bookingRepository)
        {
            _userRepository = userRepository;
            _bookingRepository = bookingRepository;
        }

        public async Task<List<UpcomingBookingDto>> Handle(GetUpcomingBookingsQuery request, CancellationToken cancellationToken)
        {
            var user = (await _userRepository.ListAsync(cancellationToken)).FirstOrDefault(u => u.UserId == request.UserId);
            if (user == null) return new List<UpcomingBookingDto>();

            var bookings = await _bookingRepository.ListAsync(cancellationToken, b => b.Provider, b => b.Customer);

            var upcomingBookings = bookings
                .Where(b => (b.CustomerId == user.Id || b.ProviderId == user.Id) && b.StartTime > DateTime.UtcNow)
                .Select(b =>
                {
                    var otherParty = b.CustomerId == user.Id ? b.Provider : b.Customer;
                    return new UpcomingBookingDto(
                        b.Id,
                        b.StartTime,
                        b.Duration,
                        otherParty.Username,
                        otherParty.AvatarUrl
                    );
                })
                .OrderBy(b => b.StartTime)
                .ToList();

            return upcomingBookings;
        }
    }
}