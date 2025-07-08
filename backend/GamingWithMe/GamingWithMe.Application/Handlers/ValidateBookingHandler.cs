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
    public class ValidateBookingHandler : IRequestHandler<ValidateBookingCommand, bool>
    {
        private readonly IAsyncRepository<User> _userRepo;
        private readonly IAsyncRepository<Booking> _bookingRepo;

        public ValidateBookingHandler(
            IAsyncRepository<User> userRepo,
            IAsyncRepository<Booking> bookingRepo)
        {
            _userRepo = userRepo;
            _bookingRepo = bookingRepo;
        }
        public async Task<bool> Handle(ValidateBookingCommand request, CancellationToken cancellationToken)
        {
            var customer = (await _userRepo.ListAsync(cancellationToken)).FirstOrDefault(x => x.UserId == request.CustomerId);
            var provicer = await _userRepo.GetByIdAsync(request.ProviderId, cancellationToken, g => g.WeeklyAvailability);

            if (provicer == null || customer == null)
                throw new InvalidOperationException("Gamer or User not found");

            if (!provicer.IsActive)
                throw new InvalidOperationException("Gamer is not currently active and cannot accept bookings.");

            if (!DateTime.TryParse(request.BookingDetailsDto.timeRange.From, out var fromTime))
                throw new InvalidOperationException("Invalid time format: From");

            if (!TimeSpan.TryParse(request.BookingDetailsDto.timeRange.Duration, out var duration))
                throw new InvalidOperationException("Invalid time format: Duration");

            if (duration <= TimeSpan.Zero)
                throw new InvalidOperationException("Invalid time range: End must be after Start");

            var date = provicer.WeeklyAvailability.FirstOrDefault(x => x.DayOfWeek == fromTime.DayOfWeek);

            if (date == null)
                throw new InvalidOperationException("Gamer is not available on the selected day.");

            var overlappingBooking = (await _bookingRepo.ListAsync(cancellationToken))
                .Where(x => x.ProviderId == provicer.Id && x.StartTime.Date == fromTime.Date)
                .Any(x =>
                    x.StartTime < fromTime + duration &&
                    fromTime < x.StartTime + x.Duration
                );

            if (overlappingBooking)
                throw new InvalidOperationException("Gamer is already booked during this time.");

            if (fromTime.TimeOfDay < date.StartTime || fromTime.TimeOfDay + duration > date.EndTime)
                throw new InvalidOperationException("Booking is outside of gamer's availability hours.");

            return true; // All checks passed
        }
    }
}
