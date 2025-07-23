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
        private readonly IAsyncRepository<User> _userrepo;
        private readonly IAsyncRepository<Booking> _bookingrepo;
        private readonly IEmailService _emailService;

        public BookingHandler(
            IAsyncRepository<User> userrepo, 
            IAsyncRepository<Booking> bookingrepo,
            IEmailService emailService)
        {
            _userrepo = userrepo;
            _bookingrepo = bookingrepo;
            _emailService = emailService;
        }

        public async Task<bool> Handle(BookingCommand request, CancellationToken cancellationToken)
        {
            var customer = (await _userrepo.ListAsync(cancellationToken)).FirstOrDefault(x => x.UserId == request.customerId);

            var provider = (await _userrepo.GetByIdAsync(request.providerId, cancellationToken, x => x.DailyAvailability, x => x.IdentityUser));

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

            var booking = new Booking(provider.Id, customer.Id, start, appointment.Duration, request.PaymentIntentId);

            await _bookingrepo.AddAsync(booking);

            // Mark appointment as unavailable
            appointment.IsAvailable = false;
            await _userrepo.Update(provider);

            // Send emails to both provider and customer
            await SendBookingConfirmationEmails(provider, customer, booking, appointment);

            return true;
        }

        private async Task SendBookingConfirmationEmails(User provider, User customer, Booking booking, UserAvailability appointment)
        {
            try
            {
                // Get provider and customer email addresses
                var providerEmail = provider.IdentityUser?.Email;
                var customerEmail = customer.IdentityUser?.Email;

                var appointmentDateTime = appointment.Date.Add(appointment.StartTime);
                var endDateTime = appointmentDateTime.Add(appointment.Duration);

                // Email variables for templates
                var emailVariables = new Dictionary<string, string>
                {
                    { "provider_name", provider.Username },
                    { "customer_name", customer.Username },
                    { "appointment_date", appointmentDateTime.ToString("MMMM dd, yyyy") },
                    { "appointment_time", appointmentDateTime.ToString("HH:mm") },
                    { "appointment_end_time", endDateTime.ToString("HH:mm") },
                    { "duration", appointment.Duration.ToString(@"hh\:mm") },
                    { "price", (appointment.Price / 100m).ToString("C") },
                    { "booking_id", booking.Id.ToString() }
                };

                // Send email to provider
                if (!string.IsNullOrEmpty(providerEmail))
                {
                    await _emailService.SendEmailAsync(
                        providerEmail,
                        "New Booking Received - Gaming Session",
                        1234567, // Replace with Mailjet template ID for provider
                        emailVariables
                    );
                }

                // Send email to customer
                if (!string.IsNullOrEmpty(customerEmail))
                {
                    await _emailService.SendEmailAsync(
                        customerEmail,
                        "Booking Confirmation - Gaming Session",
                        1234568, // Replace with Mailjet template ID for customer
                        emailVariables
                    );
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the booking process
                // add proper logging here
                Console.WriteLine($"Failed to send booking confirmation emails: {ex.Message}");
            }
        }
    }
}
