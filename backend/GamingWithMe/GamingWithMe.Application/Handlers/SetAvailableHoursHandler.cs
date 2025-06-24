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
    public class SetAvailableHoursHandler : IRequestHandler<SetAvailableHoursCommand, bool>
    {
        private readonly IAsyncRepository<Gamer> _repo;
        private readonly IGamerReadRepository _gamerReadRepository;

        public SetAvailableHoursHandler(IAsyncRepository<Gamer> repo, IGamerReadRepository gamerReadRepository)
        {
            _repo = repo;
            _gamerReadRepository = gamerReadRepository;
        }
        public async Task<bool> Handle(SetAvailableHoursCommand request, CancellationToken cancellationToken)
        {
            var user = await _gamerReadRepository.GetByIdAsync(request.userId,cancellationToken, g=> g.WeeklyAvailability);

            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            user.WeeklyAvailability.Clear();

            foreach (var kvp in request.Hours.Days)
            {
                var day = kvp.Key;
                var timeRanges = kvp.Value;

                foreach (var timeRange in timeRanges)
                {
                    if (!TimeSpan.TryParse(timeRange.From, out var start) || !TimeSpan.TryParse(timeRange.To, out var end))
                    {
                        throw new FormatException($"Invalid time format: {timeRange.From} - {timeRange.To}");
                    }

                    if (start >= end)
                    {
                        throw new ArgumentException($"Start time must be before end time: {start} - {end}");
                    }

                    var availability = new GamerAvailability
                    {
                        Id = Guid.NewGuid(),
                        DayOfWeek = day,
                        StartTime = start,
                        EndTime = end
                    };

                    user.WeeklyAvailability.Add(availability);
                }
            }

            await _repo.Update(user);

            return true;

            
        }
    }
}
