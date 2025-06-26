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
        private readonly IAsyncRepository<GamerAvailability> _arepo;

        private readonly IGamerReadRepository _gamerReadRepository;

        public SetAvailableHoursHandler(IAsyncRepository<Gamer> repo, IAsyncRepository<GamerAvailability> arepo, IGamerReadRepository gamerReadRepository)
        {
            _repo = repo;
            _gamerReadRepository = gamerReadRepository;
            _arepo = arepo;
        }
        public async Task<bool> Handle(SetAvailableHoursCommand request, CancellationToken cancellationToken)
        {
            var kalapacs = (await _repo.ListAsync(cancellationToken)).FirstOrDefault(x => x.UserId == request.userId);

            var userid = kalapacs?.Id ?? throw new Exception("hiba");

            var user = await _repo.GetByIdAsync(userid, cancellationToken, g => g.WeeklyAvailability);

            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            user.WeeklyAvailability.Clear();


            foreach (var kvp in request.Hours.Days)
            {
                var day = kvp.Key;
                var timeRange = kvp.Value;

                if (!TimeSpan.TryParse(timeRange.From, out var start) || !TimeSpan.TryParse(timeRange.Duration, out var end))
                {
                    throw new FormatException($"Invalid time format: {timeRange.From} - {timeRange.Duration}");
                }

                if (start >= end)
                {
                    throw new ArgumentException($"Start time must be before end time: {start} - {end}");
                }


                var availability = new GamerAvailability
                {
                    Id = Guid.NewGuid(),
                    GamerId = userid,
                    DayOfWeek = day,
                    StartTime = start,
                    EndTime = end
                };

                await _arepo.AddAsync(availability);


            }

            return true;


        }
    }
}
