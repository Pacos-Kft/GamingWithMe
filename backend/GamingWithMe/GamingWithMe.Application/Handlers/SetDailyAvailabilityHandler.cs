using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Handlers
{
    public class SetDailyAvailabilityHandler : IRequestHandler<SetDailyAvailabilityCommand, bool>
    {
        private readonly IAsyncRepository<User> _userRepo;
        private readonly IAsyncRepository<UserAvailability> _availabilityRepo;

        public SetDailyAvailabilityHandler(
            IAsyncRepository<User> userRepo, 
            IAsyncRepository<UserAvailability> availabilityRepo)
        {
            _userRepo = userRepo;
            _availabilityRepo = availabilityRepo;
        }

        public async Task<bool> Handle(SetDailyAvailabilityCommand request, CancellationToken cancellationToken)
        {
            var user = (await _userRepo.ListAsync(cancellationToken))
                .FirstOrDefault(x => x.UserId == request.UserId);

            if (user == null)
            {
                throw new Exception("User not found");
            }

            if (!TimeSpan.TryParse(request.Availability.StartTime, out var startTime))
                throw new FormatException($"Invalid start time format: {request.Availability.StartTime}");

            if (!TimeSpan.TryParse(request.Availability.EndTime, out var endTime))
                throw new FormatException($"Invalid end time format: {request.Availability.EndTime}");

            if (!TimeSpan.TryParse(request.Availability.SessionDuration, out var sessionDuration))
                throw new FormatException($"Invalid session duration format: {request.Availability.SessionDuration}");

            if (startTime >= endTime)
                throw new ArgumentException("Start time must be before end time");

            if (sessionDuration.TotalMinutes <= 0)
                throw new ArgumentException("Session duration must be greater than 0");

            var existingEntries = (await _availabilityRepo.ListAsync(cancellationToken))
                .Where(a => a.UserId == user.Id && a.Date.Date == request.Availability.Date.Date)
                .ToList();

            foreach (var entry in existingEntries)
            {
                await _availabilityRepo.Delete(entry);
            }

            var currentTime = startTime;
            while (currentTime.Add(sessionDuration) <= endTime)
            {
                var availability = new UserAvailability(
                    user.Id,
                    request.Availability.Date,
                    currentTime,
                    sessionDuration,
                    request.Availability.price
                );

                await _availabilityRepo.AddAsync(availability, cancellationToken);
                currentTime = currentTime.Add(sessionDuration);
            }

            return true;
        }
    }
}