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
        private readonly IAsyncRepository<Booking> _bookingRepo;

        public BookingHandler(
            IAsyncRepository<User> userRepo,
            IAsyncRepository<Booking> bookingRepo)
        {
            _userRepo = userRepo;
            _bookingRepo = bookingRepo;
        }

        public async Task<bool> Handle(BookingCommand request, CancellationToken cancellationToken)
        {
            var client = (await _userRepo.ListAsync(cancellationToken)).FirstOrDefault(x=> x.UserId == request.customerId);

            var provider = await _userRepo.GetByIdAsync(request.providerId, cancellationToken,g=> g.DailyAvailability);

            if (provider == null || client == null)
                throw new InvalidOperationException("Gamer or User not found");

            if (!provider.IsActive)
                throw new InvalidOperationException("Gamer is not currently active and cannot accept bookings.");



            if (!DateTime.TryParse(request.BookingDetailsDto.timeRange.From, out var fromTime))
                throw new InvalidOperationException("Invalid time format: From");

            if (!TimeSpan.TryParse(request.BookingDetailsDto.timeRange.Duration, out var duration))
                throw new InvalidOperationException("Invalid time format: Duration");

            if (duration <= TimeSpan.Zero)
                throw new InvalidOperationException("Invalid time range: End must be after Start");

            var date = provider.DailyAvailability.FirstOrDefault(x => x.Date.DayOfWeek == fromTime.DayOfWeek);

            if (date == null)
                throw new InvalidOperationException("Gamer is not available on the selected day.");

            var overlappingBooking = (await _bookingRepo.ListAsync(cancellationToken))
            .Where(x => x.ProviderId == provider.Id && x.StartTime.Date == fromTime.Date)
            .Any(x =>
                x.StartTime < fromTime + duration &&
                fromTime < x.StartTime + x.Duration
            );

            if (overlappingBooking)
                throw new InvalidOperationException("Gamer is already booked during this time.");

            if (fromTime.TimeOfDay < date.StartTime || fromTime.TimeOfDay + duration > date.StartTime.Add(date.Duration))
                throw new InvalidOperationException("Booking is outside of gamer's availability hours.");

            var booking = new Booking(provider.Id, client.Id, fromTime, duration,request.PaymentIntentId);

            await _bookingRepo.AddAsync(booking, cancellationToken);

            return true;
        }
    }
}
