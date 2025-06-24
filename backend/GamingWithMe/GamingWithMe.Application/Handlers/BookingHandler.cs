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
    public class BookingHandler : IRequestHandler<BookingCommand, bool>
    {
        private readonly IAsyncRepository<User> _userRepo;
        private readonly IAsyncRepository<Gamer> _gamerRepo;
        private readonly IAsyncRepository<Booking> _bookingRepo;

        public BookingHandler(
            IAsyncRepository<User> userRepo,
            IAsyncRepository<Gamer> gamerRepo,
            IAsyncRepository<Booking> bookingRepo)
        {
            _userRepo = userRepo;
            _gamerRepo = gamerRepo;
            _bookingRepo = bookingRepo;
        }

        public async Task<bool> Handle(BookingCommand request, CancellationToken cancellationToken)
        {
            var gamer = await _gamerRepo.GetByIdAsync(request.mentorId, cancellationToken);
            var user = await _userRepo.GetByIdAsync(request.clientId, cancellationToken);

            if (gamer == null || user == null)
                throw new InvalidOperationException("Gamer or User not found");


            if (!TimeSpan.TryParse(request.BookingDetailsDto.timeRange.From, out var fromTime) ||
                !TimeSpan.TryParse(request.BookingDetailsDto.timeRange.Duration, out var duration))
                throw new InvalidOperationException("Invalid time format");

            if (!DateOnly.TryParse(request.BookingDetailsDto.day, out var date))
                throw new InvalidOperationException("Invalid date");

            var timeOnly = TimeOnly.FromTimeSpan(fromTime);
            var start = date.ToDateTime(timeOnly);


            if (duration <= TimeSpan.Zero)
                throw new InvalidOperationException("Invalid time range: End must be after Start");

            var booking = new Booking(gamer.Id, user.Id, start, duration);

            await _bookingRepo.AddAsync(booking, cancellationToken);

            return true;
        }
    }
}
