using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Domain.Entities;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Handlers
{
    public class DeleteDailyAvailabilityHandler : IRequestHandler<DeleteDailyAvailabilityCommand, bool>
    {
        private readonly IAsyncRepository<User> _userRepo;
        private readonly IAsyncRepository<UserAvailability> _availabilityRepo;

        public DeleteDailyAvailabilityHandler(
            IAsyncRepository<User> userRepo,
            IAsyncRepository<UserAvailability> availabilityRepo)
        {
            _userRepo = userRepo;
            _availabilityRepo = availabilityRepo;
        }

        public async Task<bool> Handle(DeleteDailyAvailabilityCommand request, CancellationToken cancellationToken)
        {
            var user = (await _userRepo.ListAsync(cancellationToken))
                .FirstOrDefault(x => x.UserId == request.UserId);

            if (user == null)
            {
                throw new Exception("User not found");
            }

            var existingEntries = (await _availabilityRepo.ListAsync(cancellationToken))
                .Where(a => a.UserId == user.Id && a.Date.Date == request.Date.Date)
                .ToList();

            if (!existingEntries.Any())
            {
                return true; 
            }

            foreach (var entry in existingEntries)
            {
                await _availabilityRepo.Delete(entry);
            }

            return true;
        }
    }
}