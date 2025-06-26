using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Domain.Entities;
using MediatR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

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
            var user = (await _userRepo.ListAsync(cancellationToken)).FirstOrDefault(x=> x.UserId == request.clientId);

            var gamer = await _gamerRepo.GetByIdAsync(request.mentorId, cancellationToken,g=> g.WeeklyAvailability);

            if (gamer == null || user == null)
                throw new InvalidOperationException("Gamer or User not found");

            if (!gamer.IsActive)
                throw new InvalidOperationException("Gamer is not currently active and cannot accept bookings.");



            if (!DateTime.TryParse(request.BookingDetailsDto.timeRange.From, out var fromTime))
                throw new InvalidOperationException("Invalid time format: From");

            if (!TimeSpan.TryParse(request.BookingDetailsDto.timeRange.Duration, out var duration))
                throw new InvalidOperationException("Invalid time format: Duration");

            if (duration <= TimeSpan.Zero)
                throw new InvalidOperationException("Invalid time range: End must be after Start");

            var date = gamer.WeeklyAvailability.FirstOrDefault(x => x.DayOfWeek == fromTime.DayOfWeek);

            if (date == null)
                throw new InvalidOperationException("Gamer is not available on the selected day.");

            var overlappingBooking = (await _bookingRepo.ListAsync(cancellationToken))
            .Where(x => x.GamerId == gamer.Id && x.StartTime.Date == fromTime.Date)
            .Any(x =>
                x.StartTime < fromTime + duration &&
                fromTime < x.StartTime + x.Duration
            );

            if (overlappingBooking)
                throw new InvalidOperationException("Gamer is already booked during this time.");

            if (fromTime.TimeOfDay < date.StartTime || fromTime.TimeOfDay + duration > date.EndTime)
                throw new InvalidOperationException("Booking is outside of gamer's availability hours.");

            var booking = new Booking(gamer.Id, user.Id, fromTime, duration);

            await _bookingRepo.AddAsync(booking, cancellationToken);

            return true;
        }
    }
}
