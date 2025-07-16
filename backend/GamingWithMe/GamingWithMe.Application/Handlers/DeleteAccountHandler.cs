using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Handlers
{
    public class DeleteAccountHandler : IRequestHandler<DeleteAccountCommand, bool>
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAsyncRepository<User> _userRepository;
        private readonly IAsyncRepository<Booking> _bookingRepository;
        private readonly IAsyncRepository<Message> _messageRepository;

        public DeleteAccountHandler(
            UserManager<IdentityUser> userManager,
            IAsyncRepository<User> userRepository,
            IAsyncRepository<Booking> bookingRepository,
            IAsyncRepository<Message> messageRepository)
        {
            _userManager = userManager;
            _userRepository = userRepository;
            _bookingRepository = bookingRepository;
            _messageRepository = messageRepository;
        }

        public async Task<bool> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
        {
            var identityUser = await _userManager.FindByIdAsync(request.UserId);
            if (identityUser == null)
            {
                return false;
            }

            var customUser = (await _userRepository.ListAsync(cancellationToken))
                .FirstOrDefault(u => u.UserId == request.UserId);

            if (customUser != null)
            {
                // Check for any pending (future) bookings
                var allBookings = await _bookingRepository.ListAsync(cancellationToken);
                var hasPendingBookings = allBookings.Any(b =>
                    (b.ProviderId == customUser.Id || b.CustomerId == customUser.Id) &&
                    b.StartTime > DateTime.UtcNow);

                if (hasPendingBookings)
                {
                    throw new InvalidOperationException("Your account cannot be deleted because you have pending bookings. Please cancel or complete them first.");
                }

                // If no pending bookings, proceed with deleting related data
                var bookingsToDelete = allBookings
                    .Where(b => b.ProviderId == customUser.Id || b.CustomerId == customUser.Id);
                foreach (var booking in bookingsToDelete)
                {
                    await _bookingRepository.Delete(booking);
                }

                var messages = (await _messageRepository.ListAsync(cancellationToken))
                    .Where(m => m.SenderId == customUser.Id || m.ReceiverId == customUser.Id);
                foreach (var message in messages)
                {
                    await _messageRepository.Delete(message);
                }

                await _userRepository.Delete(customUser);
            }

            var result = await _userManager.DeleteAsync(identityUser);
            return result.Succeeded;
        }
    }
}