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
        private readonly IAsyncRepository<User> _userrepo;
        private readonly IAsyncRepository<Booking> _bookingrepo;


        public BookingHandler(IAsyncRepository<User> userrepo, IAsyncRepository<Booking> bookingrepo)
        {
            _userrepo = userrepo;
            _bookingrepo = bookingrepo;
        }

        public async Task<bool> Handle(BookingCommand request, CancellationToken cancellationToken)
        {
            var customer = (await _userrepo.ListAsync(cancellationToken)).FirstOrDefault(x => x.UserId == request.customerId);

            //var provider = await _context.Users
            //    .Include(u => u.DailyAvailability)
            //    .FirstOrDefaultAsync(x => x.Id == request.ProviderId, cancellationToken);

            var provider = (await _userrepo.GetByIdAsync(request.providerId, cancellationToken, x => x.DailyAvailability));

            if (provider == null || customer == null)
                throw new InvalidOperationException("Provider or customer not found");

            var appointment = provider.DailyAvailability.FirstOrDefault(x => x.Id == request.appointmentId);

            if (appointment == null)
            {
                throw new InvalidOperationException("Appointment doesnt exist at current user");
            }

            if (!appointment.IsAvailable)
            {
                throw new Exception("Appointment is already taken");
            }

            var start = appointment.Date.Add(appointment.StartTime);

            var booking = new Booking(provider.Id, customer.Id, start , appointment.Duration ,request.PaymentIntentId);

            await _bookingrepo.AddAsync(booking);

            return true;
        }
    }
}
